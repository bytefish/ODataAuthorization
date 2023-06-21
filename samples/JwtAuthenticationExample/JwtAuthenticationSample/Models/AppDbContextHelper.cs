// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace JwtAuthenticationSample.Models;

public class AppDbContextHelper
{
    private static List<Product> products = new()
    {
        new()
        {
            Id = 1,
            Name = "Macbook M1",
            Price = 3000,
            AddressId = 1,
        },
        new()
        {
            Id = 2,
            Name = "Macbook M2",
            Price = 3500,
            AddressId = 1,
        },
        new()
        {
            Id = 3,
            Name = "iPhone 14",
            Price = 1400,
            AddressId = 1,
        }
    };

    private static List<Address> addresses = new()
    {
        new()
        {
            Id = 1,
            Country = "USA",
            City = "California"
        },
        
    };

    public static void SeedDb(AppDbContext db)
    {
        if (!db.Addresses.Any())
        {
            foreach (var address in addresses)
            {
                db.Add(address);
            }

            db.SaveChanges();
        }
        
        if (!db.Products.Any())
        {
            foreach (var product in products)
            {
                db.Add(product);
            }

            db.SaveChanges();
        }

        if (!db.Users.Any())
        {
            db.Add(new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@admin.com",
                Password = "123456",
                Scopes =
                    "Products.Read Products.ReadByKey Products.Create Products.Update Products.Delete Products.ReadOrders"
            });

            db.Add(new User
            {
                Id = 2,
                Username = "user",
                Email = "user@user.com",
                Password = "123456",
                Scopes = "Products.Read Products.ReadByKey"
            });

            db.SaveChanges();
        }
    }
}