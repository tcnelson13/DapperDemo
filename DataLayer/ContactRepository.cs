using System.Data;
using System.Data.SqlClient;
using System.Transactions;
using Dapper;

namespace DataLayer;

public class ContactRepository : IContactRepository
{
    private IDbConnection db;

    public ContactRepository(string connString)
    {
        db = new SqlConnection(connString);
    }

    public Contact Add(Contact contact)
    {
        var sql =
            "INSERT INTO Contacts (FirstName, LastName, Email, Company, Title) VALUES(@FirstName, @LastName, @Email, @Company, @Title); " +
            "SELECT CAST(SCOPE_IDENTITY() as int);";
        var id = db.Query<int>(sql, contact).Single();
        contact.Id = id;
        return contact;
    }

    public Contact Find(int id)
    {
        return db.Query<Contact>("SELECT * FROM Contacts WHERE Id = @Id;", new { Id = id }).SingleOrDefault();
    }

    public Contact GetFullContact(int id)
    {
        var sql =
            "SELECT * FROM Contacts WHERE Id = @Id;" + 
            "SELECT * FROM Addresses WHERE ContactId = @Id;";

        using (var multipleResults = db.QueryMultiple(sql, new { Id = id }))
        {
            var contact = multipleResults.Read<Contact>().SingleOrDefault();

            var addresses = multipleResults.Read<Address>().ToList();
            if (contact != null && addresses != null)
            {
                contact.Addresses.AddRange(addresses);
            }
            return contact;
        }
    }

    public void Save(Contact contact)
    {
        // This is a new feature in C# 8.0 that will ensure the using statement is
        // applied at the end of the method and we don't need to indent
        // using var txScope = new TransactionScope();

        using (var txScope = new TransactionScope())
        {
            if (contact.IsNew)
            {
                Add(contact);
            }
            else
            {
                Update(contact);
            }

            foreach (var address in contact.Addresses.Where(a => !a.IsDeleted))
            {
                address.ContactId = contact.Id;
                if (address.IsNew)
                {
                    Add(address);
                }
                else
                {
                    Update(address);
                }
            }

            foreach (var address in contact.Addresses.Where(a => a.IsDeleted))
            {
                db.Execute("DELETE FROM Addresses WHERE Id = @Id", address.Id);
            }

            txScope.Complete();
        }
    }

    public Address Add(Address address)
    {
        var sql =
            "INSERT INTO Addresses (ContactId, AddressType, StreetAddress, City, StateId, PostalCode) VALUES (@ContactId, @AddressType, @StreetAddress, @City, @StateId, @PostalCode);" +
            "SELECT CAST(SCOPE_IDENTITY() as int); ";
        var id = db.Query<int>(sql, address).Single();
        address.Id = id;
        return address;
    }

    public Address Update(Address address)
    {
        db.Execute("UPDATE Addresses " +
            "SET AddressType = @AddressType, " +
            "    StreetAddress = @StreetAddress, " +
            "    City = @City, " +
            "    StateId = @StateId, " +
            "    PostalCode = @PostalCode " +
            "WHERE Id = @Id;", address);
        return address;
    }

    public List<Contact> GetAll()
    {
        return db.Query<Contact>("SELECT * FROM Contacts").ToList();
    }

    public void Remove(int id)
    {
        db.Execute("DELETE FROM Contacts WHERE Id = @Id", new { Id = id });
    }

    public Contact Update(Contact contact)
    {
        var sql =
            "UPDATE Contacts " +
            "SET FirstName = @FirstName, " +
            "    LastName = @LastName, " +
            "    Email = @Email, " +
            "    Company = @Company, " +
            "    Title = @Title " +
            "WHERE Id = @Id;";
        db.Execute(sql, contact);
        return contact;
    }
}
