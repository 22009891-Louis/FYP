using FYP.Models;
using RP.SOI.DotNet.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data;

namespace FYP.Controllers;

public class ProductController : Controller
{
    [Authorize(Roles = "admin, student")] // allow authenicated users only (no guest)
    public IActionResult ViewProductList()
    {
        List<Product> list = DBUtl.GetList<Product>("SELECT * FROM Product");
        return View(list);
    }

    [Authorize(Roles = "admin, student")]
    public IActionResult ViewProduct(string id) // search for product
    {
        string select = "SELECT * FROM Product WHERE ProdID='{0}'";
        List<Product> list = DBUtl.GetList<Product>(select, id);
        if (list.Count == 1)
        {
            return View(list[0]);
        }
        else
        {
            TempData["Message"] = "No product found with this ID";
            TempData["MsgType"] = "warning";
            return RedirectToAction("ViewProductList");
        }
    }

    [Authorize(Roles = "admin")]
    public IActionResult AddProduct()
    {
        return View();
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public IActionResult AddProduct(Product prod) // only admin can create new items
    {
        string insert = @"INSERT INTO Product(ProdID, ProdName, Price, Qty)
                        VALUES({0}, '{1}', {2}, {3}";
        int result = DBUtl.ExecSQL(insert, prod.ProdID, prod.ProdName, prod.Price, prod.Qty);
        if (result == 1)
        {
            TempData["Message"] = "Product Created";
            TempData["MsgType"] = "success";
        }
        else
        {
            TempData["Message"] = DBUtl.DB_Message;
            TempData["MsgType"] = "danger";
        }
        return RedirectToAction("ViewProductList");
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public IActionResult EditProduct(string id) // search for product
    {
        string prodSQL = @"Select ProdID, ProdName, Price, Qty FROM Product WHERE Product.ProdID = '{0}'";
        List<Product> listProd = DBUtl.GetList<Product>(prodSQL, id);

        if(listProd.Count == 1)
        {
            return View(listProd[0]);
        }
        else
        {
            TempData["Message"] = "No product found";
            TempData["MsgType"] = "warning";
            return RedirectToAction("ViewProductList");
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public IActionResult EditProduct(Product prod) // only admin can edit product
    {;
        string update = @"UPDATE Product SET ProdID = '{0}', ProdName = '{1}', Price = '{2}', Qty = '{3}' WHERE ProdID = '{0}'";
        int result = DBUtl.ExecSQL(update, prod.ProdID, prod.ProdName, prod.Price, prod.Qty);
        if (result == 1)
        {
            TempData["Message"] = "Product Updated";
            TempData["MsgType"] = "success";
        }
        else
        {
            TempData["Message"] = DBUtl.DB_Message;
            TempData["MsgType"] = "danger";
        }
        return RedirectToAction("ViewProductList");
    }

    [Authorize(Roles = "admin")]
    public IActionResult DeleteProduct(int id) // search for product & only admin can delete product
    {
        string select = @"SELECT * FROM Product WHERE ProdID = {0}";
        DataTable prodTable = DBUtl.GetTable(select, id);
        if(prodTable.Rows.Count != 1)
        {
            TempData["Message"] = "Product does not exist.";
            TempData["MsgType"] = "warning";
        }
        else
        {
            string delete = "DELETE FROM Product WHERE ProdID = {0}";
            int result = DBUtl.ExecSQL(delete, id);
            if (result == 1)
            {
                TempData["Message"] = "Product Deleted";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Message"] = DBUtl.DB_Message;
                TempData["MsgType"] = "danger";
            }
        }
        return RedirectToAction("ViewProductList");
    }
}

