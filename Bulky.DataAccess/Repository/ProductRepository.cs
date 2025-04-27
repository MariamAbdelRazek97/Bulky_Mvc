using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ProductRepository: Repository<Product>, IProductRepository
    {
        private ApplicationDBContext _db;
        public ProductRepository(ApplicationDBContext db):base(db) 
        {
              _db = db;
        }

        public void Update(Product product)
        {
            Product existProduct = _db.Products.FirstOrDefault(p => p.Id == product.Id);
            if (existProduct != null) 
            {
                existProduct.Id =   product.Id;
                existProduct.ISBN = product.ISBN;
                existProduct.ListPrice = product.ListPrice;
                existProduct.Price = product.Price;
                existProduct.Price100 = product.Price100;
                existProduct.Price50 = product.Price50;
                existProduct.Author = product.Author;
                existProduct.Description = product.Description;
                existProduct.CategoryId = product.CategoryId;
                existProduct.Title= product.Title;
                if (product.ImageUrl != null) 
                { 
                    existProduct.ImageUrl = product.ImageUrl;
                }
            }
        }
    }
}
