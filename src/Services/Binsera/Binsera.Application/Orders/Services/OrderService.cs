using Binsera.Core.Auth.Domain;
using Binsera.Core.Members.Domain;
using Binsera.Core.Orders;
using Binsera.Core.Orders.Contracts;
using Binsera.Core.Orders.Domain;
using Binsera.Core.Orders.Enums;
using Binsera.Core.Orders.Models;
using Binsera.Core.Payments.Domain;
using Binsera.Core.Payments.Enums;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Domain.Models;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using HautSphere.BuildingBlocks.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Orders.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class OrderService(IUnitOfWork unitOfWork) : IOrderService
    {
        private readonly IRepository<Order> _orderRepository = unitOfWork.Repository<Order>();
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();
        private readonly IRepository<UserLogin> _userRepository = unitOfWork.Repository<UserLogin>();
        private readonly IRepository<Payment> _paymentRepository = unitOfWork.Repository<Payment>();
        private readonly IRepository<KitMaster> _kitMasterRepository = unitOfWork.Repository<KitMaster>();

        /// <summary>
        /// The activation kit (Kit_Master id 2, priced at Rs.5,000) -- a member is only
        /// allowed one non-Rejected/non-Cancelled order for it at a time. This mirrors the frontend
        /// (SubscriptionPageComponent.SUBSCRIPTION_KIT_ID) and is enforced here too, since the API
        /// is the real gate: a member can resubmit only after their existing subscription order is
        /// rejected or cancelled, not while one is still Draft/PendingPayment/PaymentReceived/
        /// Approved/Completed.
        /// </summary>
        public async Task<long> CreateOrderAsync(CreateOrderRequest request, long memberId)
        {
            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("Order must contain at least one item.");

            var isSubscriptionOrder = request.Items.Any(x => x.ItemId == JoiningPackage.KitId);
            if (isSubscriptionOrder)
            {
                var hasOpenSubscriptionOrder = await _orderRepository.TableNoTracking
                    .Include(x => x.Items)
                    .AnyAsync(x => x.MemberId == memberId
                                   && x.Status != OrderStatus.Rejected
                                   && x.Status != OrderStatus.Cancelled
                                   && x.Items.Any(i => i.ItemId == JoiningPackage.KitId));

                if (hasOpenSubscriptionOrder)
                    throw new InvalidOperationException(
                        "You already have a subscription order that is pending or approved. " +
                        "A new subscription order can only be submitted after that one is rejected or cancelled.");
            }

            var orderNo = await GenerateOrderNoAsync();
            var order = Order.Create(request, memberId, orderNo);

            await _orderRepository.Save(order);
            return order.Id;
        }

        public async Task<IPagedList<OrderListResponse>> GetMemberOrdersAsync(
            long memberId, OrderListRequest request, CancellationToken cancellationToken)
        {
            var query = _orderRepository.TableNoTracking
                .Where(x => x.MemberId == memberId);

            query = ApplyMemberFilters(query, request.Search, request.OrderType, request.Status,
                request.FromDate, request.ToDate);

            var responseQuery = query.Select(order => new OrderListResponse
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                OrderType = order.OrderType,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                ItemCount = order.Items.Count,
                CreatedDate = order.CreatedDate
            });

            return await responseQuery
                .OrderByDescending(x => x.CreatedDate)
                .ToPagedListAsync(request.Page, request.PageSize, cancellationToken);
        }

        public async Task<OrderDetailResponse> GetOrderDetailsAsync(long orderId, long memberId)
        {
            var order = await _orderRepository.TableNoTracking
                .Include(x => x.Items)
                .Include(x => x.StatusHistory)
                .FirstOrDefaultAsync(x => x.Id == orderId && x.MemberId == memberId);

            if (order is null)
                throw new KeyNotFoundException($"Order with id {orderId} not found.");

            return MapToDetailResponse(order);
        }

        public async Task<bool> HasNoPurchasedOrderAsync(long memberId, CancellationToken cancellationToken)
        {
            var hasPurchased = await _orderRepository.TableNoTracking
                .AnyAsync(x => x.MemberId == memberId &&
                               (x.Status == OrderStatus.Approved || x.Status == OrderStatus.Completed),
                    cancellationToken);

            return !hasPurchased;
        }

        public async Task CancelOrderAsync(long orderId, long memberId)
        {
            var order = await _orderRepository.Table
                .FirstOrDefaultAsync(x => x.Id == orderId && x.MemberId == memberId);

            if (order is null)
                throw new KeyNotFoundException($"Order with id {orderId} not found.");

            if (order.Status != OrderStatus.Draft && order.Status != OrderStatus.PendingPayment)
                throw new InvalidOperationException("Only draft or pending payment orders can be cancelled.");

            order.Cancel(memberId);
            await _orderRepository.Save(order);
        }

        public async Task<AdminOrderSummaryDto> GetAdminOrderSummaryAsync(
            AdminOrderListRequest request, CancellationToken cancellationToken)
        {
            var orderQuery = BuildAdminBaseQuery(request);
            return await ComputeDashboardSummaryAsync(orderQuery, cancellationToken);
        }

        public async Task<AdminOrderListResponseDto> GetAdminOrdersAsync(
            AdminOrderListRequest request, CancellationToken cancellationToken)
        {
            var orderQuery = BuildAdminBaseQuery(request);
            var memberQuery = _memberRepository.TableNoTracking;

            var totalCount = await orderQuery.CountAsync(cancellationToken);

            var projectedQuery = BuildAdminProjectedQuery(orderQuery, memberQuery);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                projectedQuery = ApplyAdminSearch(projectedQuery, request.Search);
            }

            projectedQuery = ApplyAdminSorting(projectedQuery, request.SortBy, request.SortDirection);

            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
            var page = request.Page < 1 ? 1 : request.Page;

            var orders = await projectedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new AdminOrderListResponseDto
            {
                Orders = orders,
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<AdminOrderDetailResponse> GetAdminOrderDetailsAsync(long orderId)
        {
            var order = await _orderRepository.TableNoTracking
                .Include(x => x.Items)
                .Include(x => x.StatusHistory)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order is null)
                throw new KeyNotFoundException($"Order with id {orderId} not found.");

            var member = await _memberRepository.TableNoTracking
                .FirstOrDefaultAsync(x => x.RegNo == order.MemberId);

            var sponsor = member != null
                ? await _memberRepository.TableNoTracking
                    .FirstOrDefaultAsync(x => x.RegNo == member.IntroRegNo)
                : null;

            var kit = member != null
                ? await _kitMasterRepository.TableNoTracking
                    .FirstOrDefaultAsync(x => x.Id == member.KID)
                : null;

            var createdByUser = await _userRepository.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == order.CreatedBy);

            var approvedByUser = order.ApprovedBy.HasValue
                ? await _userRepository.TableNoTracking
                    .FirstOrDefaultAsync(x => x.Id == order.ApprovedBy.Value)
                : null;

            var paymentStatus = await _paymentRepository.TableNoTracking
                .Where(p => p.ReferenceType == ReferenceType.ORDER && p.ReferenceId == order.Id)
                .OrderByDescending(p => p.CreatedDate)
                .Select(p => (PaymentStatus?)p.Status)
                .FirstOrDefaultAsync();

            return new AdminOrderDetailResponse
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                OrderType = order.OrderType,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Remarks = order.Remarks,
                PaymentMode = order.PaymentMode,
                PaymentStatus = paymentStatus,
                TotalItems = order.Items.Count,
                CreatedDate = order.CreatedDate,
                ApprovedOn = order.ApprovedOn,
                RejectedReason = order.RejectedReason,

                Items = order.Items.Select(i => new OrderItemResponse
                {
                    Id = i.Id,
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount
                }).ToList(),

                Member = member != null ? new AdminOrderMemberInfo
                {
                    Id = member.RegNo ?? 0,
                    RegNo = member.RegNo,
                    RegistrationNumber = member.LoginID,
                    MemberName = (member.FName ?? "") + " " + (member.LName ?? ""),
                    Mobile = member.Mobile,
                    Email = member.EmailID,
                    Address = member.Address,
                    City = member.OCity,
                    State = member.OState,
                    PinCode = member.PinCode,
                    PackageName = kit != null ? kit.KitDesc : null,
                    Rank = member.BLevel,
                    MemberStatus = member.Active == true ? "Active" : "Inactive"
                } : null,

                Sponsor = sponsor != null ? new AdminOrderSponsorInfo
                {
                    Id = sponsor.RegNo ?? 0,
                    RegNo = sponsor.RegNo,
                    RegistrationNumber = sponsor.LoginID,
                    SponsorName = (sponsor.FName ?? "") + " " + (sponsor.LName ?? ""),
                    Mobile = sponsor.Mobile
                } : null,

                StatusHistory = order.StatusHistory
                    .OrderBy(x => x.ChangedOn)
                    .Select(h => new OrderStatusHistoryResponse
                    {
                        FromStatus = h.FromStatus,
                        ToStatus = h.ToStatus,
                        Remarks = h.Remarks,
                        ChangedOn = h.ChangedOn
                    }).ToList(),

                Audit = new AdminOrderAuditInfo
                {
                    CreatedByName = createdByUser != null ? createdByUser.UserName : null,
                    ApprovedByName = approvedByUser != null ? approvedByUser.UserName : null
                }
            };
        }

        public async Task ApproveOrderAsync(long orderId, long adminId)
        {
            var order = await _orderRepository.Table
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order is null)
                throw new KeyNotFoundException($"Order with id {orderId} not found.");

            if (order.Status != OrderStatus.PaymentReceived)
                throw new InvalidOperationException("Only payment received orders can be approved.");

            order.Approve(adminId);
            await _orderRepository.Save(order);
        }

        public async Task VerifyPaymentAsync(long orderId, long adminId)
        {
            var order = await _orderRepository.Table
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order is null)
                throw new KeyNotFoundException($"Order with id {orderId} not found.");

            if (order.Status != OrderStatus.PendingPayment)
                throw new InvalidOperationException("Only pending payment orders can be verified.");

            order.SetPaymentReceived(adminId);
            await _orderRepository.Save(order);
        }

        public async Task RejectOrderAsync(long orderId, long adminId, RejectOrderRequest request)
        {
            var order = await _orderRepository.Table
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order is null)
                throw new KeyNotFoundException($"Order with id {orderId} not found.");

            if (order.Status != OrderStatus.PendingPayment && order.Status != OrderStatus.PaymentReceived)
                throw new InvalidOperationException("Only pending payment or payment received orders can be rejected.");

            order.Reject(adminId, request.Reason);
            await _orderRepository.Save(order);
        }

        #region Private Methods

        private IQueryable<Order> BuildAdminBaseQuery(AdminOrderListRequest request)
        {
            var query = _orderRepository.TableNoTracking.AsQueryable();

            if (request.OrderType.HasValue)
                query = query.Where(x => x.OrderType == request.OrderType.Value);

            if (request.OrderStatus.HasValue)
                query = query.Where(x => x.Status == request.OrderStatus.Value);

            if (request.DateFrom.HasValue)
            {
                var dateFrom = request.DateFrom.Value;
                query = query.Where(o =>
                    _paymentRepository.TableNoTracking.Any(p =>
                        p.ReferenceType == ReferenceType.ORDER &&
                        p.ReferenceId == o.Id &&
                        p.PaidOn >= dateFrom));
            }

            if (request.DateTo.HasValue)
            {
                var dateTo = request.DateTo.Value;
                query = query.Where(o =>
                    _paymentRepository.TableNoTracking.Any(p =>
                        p.ReferenceType == ReferenceType.ORDER &&
                        p.ReferenceId == o.Id &&
                        p.PaidOn <= dateTo));
            }

            if (!string.IsNullOrWhiteSpace(request.LoginId))
            {
                var loginId = request.LoginId.Trim();
                query = query.Where(o =>
                    _memberRepository.TableNoTracking.Any(m =>
                        m.RegNo == o.MemberId &&
                        m.LoginID == loginId));
            }

            if (request.PaymentStatus.HasValue)
            {
                query = query.Where(o =>
                    _paymentRepository.TableNoTracking.Any(p =>
                        p.ReferenceType == ReferenceType.ORDER &&
                        p.ReferenceId == o.Id &&
                        p.Status == request.PaymentStatus.Value));
            }

            return query;
        }

        private static IQueryable<AdminOrderListDto> BuildAdminProjectedQuery(
            IQueryable<Order> orderQuery, IQueryable<Member> memberQuery)
        {
            return from order in orderQuery
                   join member in memberQuery on order.MemberId equals member.RegNo
                   join sponsor in memberQuery on member.IntroRegNo equals sponsor.RegNo
                       into sponsorJoin
                   from sponsor in sponsorJoin.DefaultIfEmpty()
                   select new AdminOrderListDto
                   {
                       Id = order.Id,
                       OrderNo = order.OrderNo,
                       MemberId = member.RegNo ?? 0,
                       RegistrationNumber = member.LoginID,
                       MemberName = (member.FName ?? "") + " " + (member.LName ?? ""),
                       SponsorId = sponsor != null ? sponsor.RegNo : (long?)null,
                       SponsorName = sponsor != null
                           ? (sponsor.FName ?? "") + " " + (sponsor.LName ?? "") : null,
                       Mobile = member.Mobile,
                       Email = member.EmailID,
                       Package = null,
                       Rank = member.BLevel,
                       MemberStatus = member.Active == true ? "Active" : "Inactive",
                       OrderType = order.OrderType,
                       TotalAmount = order.TotalAmount,
                       PaymentMode = order.PaymentMode,
                       OrderStatus = order.Status,
                       TotalItems = order.Items.Count,
                       CreatedDate = order.CreatedDate,
                       PendingSinceDays = order.Status == OrderStatus.PendingPayment
                           ? (int)(DateTime.UtcNow - order.CreatedDate).TotalDays
                           : (int?)null,
                       ApprovedDate = order.ApprovedOn
                   };
        }

        private IQueryable<AdminOrderListDto> ApplyAdminSearch(IQueryable<AdminOrderListDto> query, string search)
        {
            var s = search.Trim().ToLower();
            return query.Where(x =>
                x.OrderNo.ToLower().Contains(s) ||
                x.MemberId.ToString().Contains(s) ||
                (x.RegistrationNumber != null && x.RegistrationNumber.ToString().Contains(s)) ||
                x.MemberName.ToLower().Contains(s) ||
                (x.SponsorName != null && x.SponsorName.ToLower().Contains(s)) ||
                (x.Mobile != null && x.Mobile.ToLower().Contains(s)) ||
                (x.Email != null && x.Email.ToLower().Contains(s)) ||
                _paymentRepository.TableNoTracking.Any(p =>
                    p.ReferenceType == ReferenceType.ORDER &&
                    p.ReferenceId == x.Id &&
                    p.TransactionReference != null &&
                    p.TransactionReference.ToLower().Contains(s)));
        }

        private static IQueryable<AdminOrderListDto> ApplyAdminSorting(
            IQueryable<AdminOrderListDto> query, string sortBy, string sortDirection)
        {
            var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy?.ToLower() switch
            {
                "orderno" => descending
                    ? query.OrderByDescending(x => x.OrderNo)
                    : query.OrderBy(x => x.OrderNo),
                "membername" => descending
                    ? query.OrderByDescending(x => x.MemberName)
                    : query.OrderBy(x => x.MemberName),
                "amount" => descending
                    ? query.OrderByDescending(x => x.TotalAmount)
                    : query.OrderBy(x => x.TotalAmount),
                "paymentstatus" => descending
                    ? query.OrderByDescending(x => x.PaymentStatus)
                    : query.OrderBy(x => x.PaymentStatus),
                "orderstatus" => descending
                    ? query.OrderByDescending(x => x.OrderStatus)
                    : query.OrderBy(x => x.OrderStatus),
                _ => descending
                    ? query.OrderByDescending(x => x.CreatedDate)
                    : query.OrderBy(x => x.CreatedDate)
            };
        }

        private async Task<AdminOrderSummaryDto> ComputeDashboardSummaryAsync(
            IQueryable<Order> orderQuery, CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var totalOrders = await orderQuery.CountAsync(cancellationToken);

            if (totalOrders == 0)
                return new AdminOrderSummaryDto();

            var pendingPaymentOrders = await orderQuery.CountAsync(x => x.Status == OrderStatus.PendingPayment, cancellationToken);
            var paymentReceivedOrders = await orderQuery.CountAsync(x => x.Status == OrderStatus.PaymentReceived, cancellationToken);
            var approvedOrders = await orderQuery.CountAsync(x => x.Status == OrderStatus.Approved, cancellationToken);
            var rejectedOrders = await orderQuery.CountAsync(x => x.Status == OrderStatus.Rejected, cancellationToken);
            var completedOrders = await orderQuery.CountAsync(x => x.Status == OrderStatus.Completed, cancellationToken);
            var cancelledOrders = await orderQuery.CountAsync(x => x.Status == OrderStatus.Cancelled, cancellationToken);
            var todayOrders = await orderQuery.CountAsync(x => x.CreatedDate >= today, cancellationToken);
            var thisMonthOrders = await orderQuery.CountAsync(x => x.CreatedDate >= monthStart, cancellationToken);
            var totalOrderAmount = await orderQuery.SumAsync(x => x.TotalAmount, cancellationToken);
            var pendingPaymentAmount = await orderQuery
                .Where(x => x.Status == OrderStatus.PendingPayment)
                .SumAsync(x => x.TotalAmount, cancellationToken);
            var receivedPaymentAmount = await orderQuery
                .Where(x => x.Status == OrderStatus.PaymentReceived ||
                            x.Status == OrderStatus.Approved ||
                            x.Status == OrderStatus.Completed)
                .SumAsync(x => x.TotalAmount, cancellationToken);

            return new AdminOrderSummaryDto
            {
                TotalOrders = totalOrders,
                PendingPaymentOrders = pendingPaymentOrders,
                PaymentReceivedOrders = paymentReceivedOrders,
                ApprovedOrders = approvedOrders,
                RejectedOrders = rejectedOrders,
                CompletedOrders = completedOrders,
                CancelledOrders = cancelledOrders,
                TodayOrders = todayOrders,
                ThisMonthOrders = thisMonthOrders,
                TotalOrderAmount = totalOrderAmount,
                PendingPaymentAmount = pendingPaymentAmount,
                ReceivedPaymentAmount = receivedPaymentAmount
            };
        }

        private static IQueryable<Order> ApplyMemberFilters(IQueryable<Order> query,
            string search, OrderType? orderType, OrderStatus? status,
            DateTime? fromDate, DateTime? toDate)
        {
            if (orderType.HasValue)
                query = query.Where(x => x.OrderType == orderType.Value);

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (fromDate.HasValue)
                query = query.Where(x => x.CreatedDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.CreatedDate <= toDate.Value);

            return query;
        }

        private async Task<string> GenerateOrderNoAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"ORD{today}";
            var lastOrder = await _orderRepository.TableNoTracking
                .Where(x => x.OrderNo.StartsWith(prefix))
                .OrderByDescending(x => x.OrderNo)
                .FirstOrDefaultAsync();

            var sequence = lastOrder != null
                ? int.Parse(lastOrder.OrderNo.Substring(prefix.Length)) + 1
                : 1;

            return $"{prefix}{sequence:D4}";
        }

        private static OrderDetailResponse MapToDetailResponse(Order order)
        {
            return new OrderDetailResponse
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                OrderType = order.OrderType,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Remarks = order.Remarks,
                PaymentMode = order.PaymentMode,
                CreatedDate = order.CreatedDate,
                ApprovedOn = order.ApprovedOn,
                RejectedReason = order.RejectedReason,

                Items = order.Items.Select(i => new OrderItemResponse
                {
                    Id = i.Id,
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount
                }).ToList(),

                StatusHistory = order.StatusHistory
                    .OrderBy(x => x.ChangedOn)
                    .Select(h => new OrderStatusHistoryResponse
                    {
                        FromStatus = h.FromStatus,
                        ToStatus = h.ToStatus,
                        Remarks = h.Remarks,
                        ChangedOn = h.ChangedOn
                    }).ToList()
            };
        }

        #endregion
    }
}
