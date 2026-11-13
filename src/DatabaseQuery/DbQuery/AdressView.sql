create view AddressView as
select ct.CID Id, c.CID CountryId, c.countryName CountryName,
s.SID StateId,s.stateName,ct.cityName,ct.Pincode from COUNTRY c 
join State s on c.CID=s.CID and s.sActive=1 and c.cActive =1
join CITY ct on ct.SID=s.SID and ct.Active=1