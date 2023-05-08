using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Dapper;

namespace DataLayer
{
	public class ContactRepositorySP : IContactRepository
	{
        private IDbConnection db;

        public ContactRepositorySP(string connString)
        {
            db = new SqlConnection(connString);
        }

        public Contact Add(Contact contact)
        {
            throw new NotImplementedException();
        }

        public Contact Find(int id)
        {
            return db.Query<Contact>("GetContact", new { Id = id }, commandType: CommandType.StoredProcedure).SingleOrDefault();
        }

        public List<Contact> GetAll()
        {
            throw new NotImplementedException();
        }

        public Contact GetFullContact(int id)
        {
            using (var multipleResults = db.QueryMultiple("GetContact", new { Id = id }, commandType: CommandType.StoredProcedure))
            {
                var contact = multipleResults.Read<Contact>().SingleOrDefault();
                var addresses = multipleResults.Read<Address>().ToList();

                if (contact!= null && addresses != null)
                {
                    contact.Addresses.AddRange(addresses);
                }

                return contact;
            }
        }

        public void Remove(int id)
        {
            db.Execute("DeleteContact", new { Id = id }, commandType: CommandType.StoredProcedure);
        }

        public void Save(Contact contact)
        {
            using var txScope = new TransactionScope();
            var parameters = new DynamicParameters();
            parameters.Add("@Id", contact.Id, DbType.Int32, direction: ParameterDirection.InputOutput);
            parameters.Add("@FirstName", contact.FirstName);
            parameters.Add("@LastName", contact.LastName);
            parameters.Add("@Company", contact.Company);
            parameters.Add("@Title", contact.Title);
            parameters.Add("@Email", contact.Email);
            db.Execute("SaveContact", parameters, commandType: CommandType.StoredProcedure);
            contact.Id = parameters.Get<int>("@Id");

            foreach(var address in contact.Addresses.Where(a => !a.IsDeleted))
            {
                address.ContactId = contact.Id;

                var addressParams = new DynamicParameters(new
                {
                    ContactId = address.ContactId,
                    AddressType = address.AddressType,
                    StreetAddress = address.StreetAddress,
                    City = address.City,
                    StateId = address.StateId,
                    PostalCode = address.PostalCode
                });
                addressParams.Add("@Id", address.Id, DbType.Int32, ParameterDirection.InputOutput);
                db.Execute("SaveAddress", addressParams, commandType: CommandType.StoredProcedure);
                address.Id = addressParams.Get<int>("@Id");
            }

            foreach(var address in contact.Addresses.Where(a => a.IsDeleted))
            {
                db.Execute("DeleteAddress", new { Id = address.Id }, commandType: CommandType.StoredProcedure);
            }

            txScope.Complete();
        }

        public Contact Update(Contact contact)
        {
            throw new NotImplementedException();
        }
    }
}

