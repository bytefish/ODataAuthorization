using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore3ODataPermissionsSample.Models
{
    [ComplexType]
    public class Address
    {
        public string City { get; set; }

        public string Street { get; set; }
    }
}
