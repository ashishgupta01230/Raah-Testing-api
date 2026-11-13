alter Procedure dbo.[Sp_viewprofile] 
@regno bigint                                            
AS                                            
Begin                                            
Select distinct mm.Regno,mm.Aadhaarno, mm.ID,isnull(mm.profileImage,'profile-pic.jpg') as 'profileImage',          
fat_husname=case when isnull( mm.fat_husname,'')='' or isnull( mm.fat_husname,'NULL')='' then 'N/A' else  mm.fat_husname end  ,          
Convert(varchar ,CONVERT(date, mm.regdate, 113),113) as 'RegistrationDate' ,       
Case Convert(varchar ,CONVERT(date, mm.dob, 113),113) when '01 Jan 1990' then 'N/A' when Convert(varchar ,CONVERT(date, mm.dob, 113),113) then Convert(varchar ,CONVERT(date, mm.dob, 113),113) end   as 'birthday' ,                        
mm.loginid , Isnull(mm.title,'')+' '+isnull(mm.fname,'')+' '+isnull(mm.lname,'') as 'Name' ,                               
mm.dob , Case mm.Gender when 1 then 'Male' when 0 then 'Female' end   as 'Gender',   Case mm.maritalstatus when 1 then 'Unmarried' when 0 then 'Married' end   as 'maritalstatus',                                            
mobile=case when isnull(  mm.mobile,'')='' or isnull(  mm.mobile,'NULL')='' then 'N/A' else  mm.mobile end  ,                              
emailID=case when isnull( mm.emailID,'')='' or isnull( mm.emailID,'NULL')='' then 'N/A' else  mm.emailID end ,                              
panno=case when isnull(mm.mempan,'')='' or isnull(mm.mempan,'NULL')='' then 'N/A' else mm.mempan end,                               
cc.CountryName,mm.cid,mm.sid,mm.oState ,         
NomName=case when isnull(  mm.NomName,'')='' or isnull(  mm.NomName,'NULL')='' then 'N/A' else  mm.NomName end ,     
remarks=case when isnull(  mm.remarks,'')='' or isnull(  mm.remarks,'NULL')='' then 'N/A' else  mm.remarks end ,            
relation=case when isnull(  mm.relation,'')='' or isnull(  mm.relation,'NULL')='' then 'N/A' else  mm.relation end,            
mm.Ctid ,mm.oCity ,mm.pincode ,mm.address, mm.District,                                              
case  isnull(mm.LandMark,'Empty') when 'Empty' then '' else  'Near by '+mm.landmark end as 'landmark',                              
firstS_Qid=mm.firstS_Qid,firstS_ANS=case when isnull(mm.firstS_ANS,'')='' or isnull(mm.firstS_ANS,'NULL')='' then 'N/A' else mm.firstS_ANS end,          
coreBank=case when isnull(mm.coreBank,'')='' or isnull(mm.coreBank,'NULL')='' then 'N/A' else mm.coreBank end            
,mm.paymode,convert(varchar(10),mm.refDate,103)  as refDate ,          
refNo=case when isnull(  mm.refNo,'')='' or isnull(  mm.refNo,'NULL')='' then 'N/A' else  mm.refNo end,            
bankName=case when isnull(  mm.bankName,'')='' or isnull(  mm.bankName,'NULL')='' then 'N/A' else  mm.bankName end,            
BranchName=case when isnull(  mm.BranchName,'')='' or isnull(  mm.BranchName,'NULL')='' then 'N/A' else  mm.BranchName end,            
BranchCity=case when isnull(  mm.BranchCity,'')='' or isnull(  mm.BranchCity,'NULL')='' then 'N/A' else  mm.BranchCity end,            
PayChqNo=case when isnull(  mm.PayChqNo,'')='' or isnull(  mm.PayChqNo,'NULL')='' then 'N/A' else  mm.PayChqNo end,            
CompanyBank=case when isnull(  mm.CompanyBank,'')='' or isnull(  mm.CompanyBank,'NULL')='' then 'N/A' else  mm.CompanyBank end,            
BranchCity=case when isnull(  mm.BranchCity,'')='' or isnull(  mm.BranchCity,'NULL')='' then 'N/A' else  mm.BranchCity end,            
 mm.PayChqDate                        
,branchCode=case when isnull(mm.branchCode,'')='' or isnull(mm.branchCode,'NULL')='' then 'N/A' else mm.branchCode end ,          
IFSCCode=case when isnull(mm.IFSCCode,'')='' or isnull(mm.IFSCCode,'NULL')='' then 'N/A' else mm.IFSCCode end ,                              
accountNo=case when isnull(mm.accountNo,'')='' or isnull(mm.accountNo,'NULL')='' then 'N/A' else mm.accountNo end ,branchaddress=case when isnull(mm.branchCode,'')='' or                               
isnull(mm.branchCode,'NULL')='' then 'N/A' else mm.branchCode end,ISDCode=case when isnull( mm.ISDCode,'')='' or isnull( mm.ISDCode,'NULL')='' then '' else  mm.ISDCode end          
,mm.Phone, mm2.loginid as 'Sponsor Username',  isnull(mm2.title,'')+' '+ isnull(mm2.fName,'')+ ' '+ISNULL(mm2.lname,'') as 'Sponsor Name',      
st.statename ,D.Distname,ct.cityname,(mm.kitPrice ) as 'EMI Plan',   (P.PropLocation) as 'Property Location',      
Father_Husband=case when isnull( mm.fat_husname,'')='' or isnull( mm.fat_husname,'NULL')='' then 'N/A' else  mm.fat_husname end,      
ml.epassword,ul.Password      
from Member_Master mm           
join member_Master mm2 on mm.introRegNo = mm2.regno                      
left outer join Country cc on  mm.cid=cc.cid           
left outer join   State St on St.SID=mm.sid                     
left outer join   DISTRICT D on D.Distid=mm.District             
left outer join   CITY Ct on Ct.ctid=mm.ctid                   
left join mst_Property P on P.PropLocationID=mm.PropLocationID                           
inner join member_login ml on ml.Regno=mm.regno 
left join UserLogin ul on ul.Id=mm2.id
where MM.regno=@regno                                         
 End 
