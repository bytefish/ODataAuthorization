using AspNetCore3ODataPermissionsSample.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore3ODataPermissionsSample.Controllers
{
    public class CustomersController : ODataController
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            // NOTE: without the NoTracking setting, the query for $select=HomeAddress will throw an exception
            // A tracking query projects owned entity without corresponding owner in result. Owned entities cannot be tracked without their owner...
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            return Ok(_context.Customers);
        }

        [EnableQuery]
        public IActionResult Get(int key)
        {
            return Ok(_context.Customers.FirstOrDefault(c => c.Id == key));
        }

        /// <summary>
        /// If testing in IISExpress with the POST request to: http://localhost:2087/test/my/a/Customers
        /// Content-Type : application/json
        /// {
        ///    "Name": "Jonier","
        /// }
        /// 
        /// Check the reponse header, you can see 
        /// "Location" : "http://localhost:2087/test/my/a/Customers(0)"
        /// </summary>
        [EnableQuery]
        public IActionResult Post([FromBody]Customer customer)
        {
            return Created(customer);
        }

        public IActionResult Delete(int key)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.Id == key);
            _context.Customers.Remove(customer);
            return Ok(customer);
        }

        [HttpGet("odata/GetTopCustomer")]
        public IActionResult GetTopCustomer()
        {
            return Ok(_context.Customers.FirstOrDefault());
        }

        [HttpGet("odata/Customers({key})/GetAge")]
        public IActionResult GetAge(int key)
        {
            return Ok(_context.Customers.Find(key).Id + 20);
        }

        [HttpGet("odata/Customers({key})/Orders")]
        public IActionResult GetCustomerOrders(int key)
        {
            return Ok(_context.Customers.Find(key).Orders);
        }

        [HttpGet("odata/Customers({key})/Orders({relatedKey})")]
        public IActionResult GetCustomerOrder(int key, int relatedKey)
        {
            return Ok(_context.Customers.Find(key).Orders.FirstOrDefault(o => o.Id == relatedKey));
        }

        [HttpGet("odata/Customers({key})/Orders({relatedKey})/Title")]
        public IActionResult GetOrderTitleByKey(int key, int relatedKey)
        {
            return Ok(_context.Customers.Find(key)?.Orders.FirstOrDefault(o => o.Id == relatedKey)?.Title);
        }

        [HttpGet("odata/Customers({key})/Orders({relatedKey})/$ref")]
        public IActionResult GetCustomerOrderByKeyRef(int key, [FromODataUri] int relatedKey)
        {
            return Ok(_context.Customers.Find(key)?.Orders.FirstOrDefault(o => o.Id == relatedKey));
        }

        [HttpGet("odata/Customers({key})/Order")]
        public IActionResult GetOrder(int key)
        {
            return Ok(_context.Customers.Find(key).Order);
        }

        [HttpGet("odata/Customers({key})/Order/Title")]
        public IActionResult GetOrderTitle(int key)
        {
            return Ok(_context.Customers.Find(key)?.Order?.Title);
        }
    }
}
