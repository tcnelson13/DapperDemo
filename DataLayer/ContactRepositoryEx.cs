﻿using System;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace DataLayer
{
	public class ContactRepositoryEx
	{
        private IDbConnection db;

        public ContactRepositoryEx(string connString)
        {
            db = new SqlConnection(connString);
        }

        public async Task<List<Contact>> GetAllAsync()
        {
            var contacts = await db.QueryAsync<Contact>("SELECT * FROM Contacts");
            return contacts.ToList();
        }

        public List<Contact> GetAllContactsWithAddresses()
        {
            var sql = "SELECT * FROM Contacts AS C INNER JOIN Addresses AS A ON A.ContactId = C.Id;";

            var contactDict = new Dictionary<int, Contact>();

            var contacts = db.Query<Contact, Address, Contact>(sql, (contact, address) =>
            {
                if (!contactDict.TryGetValue(contact.Id, out var currentContact))
                {
                    currentContact = contact;
                    contactDict.Add(currentContact.Id, currentContact);
                }
                currentContact.Addresses.Add(address);
                return currentContact;
            });
            return contacts.Distinct().ToList();
        }

        public List<Address> GetAddressesByState(int stateId)
        {
            return db.Query<Address>("SELECT * FROM Addresses WHERE StateId = {=stateId}", new { stateId }).ToList();
        }

        public int BulkInsertContacts(List<Contact> contacts)
        {
            var sql =
                "INSERT INTO Contacts (FirstName, LastName, Email, Company, Title) VALUES(@FirstName, @LastName, @Email, @Company, @Title); " +
                "SELECT CAST(SCOPE_IDENTITY() as int);";

            return db.Execute(sql, contacts);
        }

        public List<Contact> GetContactsById(params int[] ids)
        {
            return db.Query<Contact>("SELECT * FROM Contacts WHERE Id IN @Ids", new { Ids = ids }).ToList();
        }

        public List<dynamic> GetDynamicContactsById(params int[] ids)
        {
            return db.Query("SELECT * FROM Contacts WHERE Id IN @Ids", new { Ids = ids }).ToList();
        }
    }
}

