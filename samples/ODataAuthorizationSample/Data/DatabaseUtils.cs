using AspNetCore3ODataPermissionsSample.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ODataAuthorizationSample.Data
{
    public static class DatabaseUtils
    {
        public static void CreateDatabaseAndSampleData(IServiceProvider services)
        {
            using (var serviceScope = services.CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<AppDbContext>())
                {
                    context.Database.EnsureDeleted();
                    context.Database.EnsureCreated();

                    IList<Customer> customers = new List<Customer>
                    {
                        new Customer
                        {
                            Name = "Jonier",
                            HomeAddress = new Address { City = "Redmond", Street = "156 AVE NE"},
                            FavoriteAddresses = new List<Address>
                            {
                                new Address { City = "Redmond", Street = "256 AVE NE"},
                                new Address { City = "Redd", Street = "56 AVE NE"},
                            },
                            Order = new Order { Title = "104m" },
                            Orders = Enumerable.Range(0, 2).Select(e => new Order { Title = "abc" + e }).ToList()
                        },
                        new Customer
                        {
                            Name = "Sam",
                            HomeAddress = new Address { City = "Bellevue", Street = "Main St NE"},
                            FavoriteAddresses = new List<Address>
                            {
                                new Address { City = "Red4ond", Street = "456 AVE NE"},
                                new Address { City = "Re4d", Street = "51 NE"},
                            },
                            Order = new Order { Title = "Zhang" },
                            Orders = Enumerable.Range(0, 2).Select(e => new Order { Title = "xyz" + e }).ToList()
                        },
                        new Customer
                        {
                            Name = "Peter",
                            HomeAddress = new Address {  City = "Hollewye", Street = "Main St NE"},
                            FavoriteAddresses = new List<Address>
                            {
                                new Address { City = "R4mond", Street = "546 NE"},
                                new Address { City = "R4d", Street = "546 AVE"},
                            },
                            Order = new Order { Title = "Jichan" },
                            Orders = Enumerable.Range(0, 2).Select(e => new Order { Title = "ijk" + e }).ToList()
                        },
                    };

                    foreach (var customer in customers)
                    {
                        context.Customers.Add(customer);

                        context.Orders.Add(customer.Order);
                        context.Orders.AddRange(customer.Orders);
                    }

                    context.SaveChanges();
                }
            }
        }
    }
}