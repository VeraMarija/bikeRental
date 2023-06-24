﻿using bikeRental.Application.Models.Station;
using bikeRental.Application;
using bikeRental.Application.Services;
using bikeRental.Application.Services.Impl;
using Microsoft.AspNetCore.Mvc;
using bikeRental.Application.Models.Order;
using Microsoft.AspNetCore.Authorization;
using System.Data.Entity.Infrastructure;
using bikeRental.Core.Entities;
using Microsoft.AspNet.Identity;
using bikeRental.Core.Identity;
using bikeRental.Application.Models.User;
using System.Xml.Linq;
using bikeRental.Application.Models.Bicycle;
using AutoMapper;
using bikeRental.Core.Enums;

namespace bikeRental.Frontend.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly IBicycleService _bicycleService;
        private readonly IStationService _stationService;
        private readonly IMapper _mapper;


        public OrdersController(IOrderService orderService, IUserService userService, IBicycleService bicycleService, IStationService stationService, IMapper mapper)
        {
            _orderService = orderService;
            _userService = userService;
            _bicycleService = bicycleService;
            _stationService = stationService;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize(Roles = ("Administrator"))]
        public IActionResult Index(string currentCategory, string sortOrder, DateTime searchDateFrom, DateTime searchDateTo, int? pageNumber)
        {
            if (pageNumber == null)
            {
                pageNumber = 1;
            }
          
            ViewData["SearchDateFrom"] = searchDateFrom;
            ViewData["SearchDateTo"] = searchDateTo;
            ViewData["SortOrder"] = sortOrder;
            int pageSize = 6;

            var orders = _orderService.GetAll();

            orders = _orderService.SortingSelection(orders, sortOrder);
            if (searchDateTo != DateTime.MinValue)
            {
                orders = _orderService.SearchSelection(orders, searchDateFrom, searchDateTo);
            }
            
            return View("/Pages/Orders/Index.cshtml", PaginatedList<OrderResponse>.Create(orders, pageNumber ?? 1, pageSize));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UserIndex(string sortOrder, int? pageNumber)
        {
            var userID = Guid.Parse(User.Identity.GetUserId());

            ViewData["SortOrder"] = sortOrder;

            int pageSize = 6;

            var orders = await _orderService.GetByCustomer(userID);
         
            orders = _orderService.SortingSelection(orders, sortOrder);

            return View("/Pages/Orders/UserIndex.cshtml", PaginatedList<OrderResponse>.Create(orders, pageNumber ?? 1, pageSize));
        }

        [Authorize]
        public async Task<IActionResult> Create(Guid? bicycleId)
        {
            var userID = Guid.Parse(User.Identity.GetUserId());
            if (bicycleId == null )
            {
                return BadRequest();
            }
         
            var order = new OrderModel
            {
                RentalStartTime = DateTime.Now,
                RentalEndTime = DateTime.Now,
                RentalPrice = 0,
                Customer = await _userService.GetByIdAsync(userID),
                Bicycle = await _bicycleService.GetByIdAsync(bicycleId)
            };
            return View("/Pages/Orders/Create.cshtml", order);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(OrderModel orderModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var bicycle = await _bicycleService.GetByIdAsync(orderModel.Bicycle.Id);
                    bicycle.Status = BikeStatus.InUse;
                    await _bicycleService.UpdateAsync(bicycle);
                    await _orderService.AddAsync(orderModel, orderModel.Customer.Id, orderModel.Bicycle.Id);
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("-.....error..");
                System.Diagnostics.Debug.WriteLine(ex);
                ModelState.AddModelError("", "Unable to save changes. " + ex);
            }
            return RedirectToAction(nameof(UserIndex));
        }

        [Authorize]
        public async Task<IActionResult> Finish(Guid? orderId, Guid bicycleId, Guid stationId)
        {
            ViewData["Stations"] = _stationService.GetAll();
       
            var userID = Guid.Parse(User.Identity.GetUserId());
            if (orderId == null)
            {
                return BadRequest();
            }
            var order = await _orderService.GetByIdAsync(orderId, userID, bicycleId);
            var station = await _stationService.GetByIdAsync(stationId);
            order.Bicycle.Station = station;
            order.RentalEndTime = DateTime.Now;
            return View("/Pages/Orders/Finish.cshtml", order);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Finish(OrderModel orderModel, Guid stationId)
        {
            var user = await _userService.GetByIdAsync(Guid.Parse(User.Identity.GetUserId()));      

            if(user.Id != orderModel.Customer.Id)
            {
                return BadRequest();
            }  
            
            try
            {
                if (ModelState.IsValid)
                {
                    orderModel.RentalPrice = _orderService.GetRentalPrice(orderModel.RentalStartTime, orderModel.RentalEndTime, orderModel.Bicycle.Price);
                    var bicycle = await _bicycleService.GetByIdAsync(orderModel.Bicycle.Id);
                    bicycle.Status = BikeStatus.Available;
                    if(orderModel.Bicycle.Station.Id != stationId)
                    {   var station = await _stationService.GetByIdAsync(stationId);
                        bicycle.Station = station;                 
                    }
                    await _bicycleService.UpdateAsync(bicycle);
                    await _orderService.UpdateAsync(orderModel);
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("-.....error..");
                System.Diagnostics.Debug.WriteLine(ex);
                ModelState.AddModelError("", "Unable to save changes. " + ex);
            }
            return RedirectToAction(nameof(UserIndex));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(Guid? id, Guid bicycleId, Guid customerId, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var order = await _orderService.GetByIdAsync(id, customerId, bicycleId);

            if (order == null)
            {
                return NotFound();
            }

            if (saveChangesError.GetValueOrDefault())
            {
                ViewData["ErrorMessage"] =
                    "Delete failed. Try again, and if the problem persists " +
                    "see your system administrator.";
            }

            return View("/Pages/Orders/Delete.cshtml", order);
        }


        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id, Guid bicycleId, Guid customerId)
        {
            var order = await _orderService.GetByIdAsync(id, customerId, bicycleId);     
            var user = await _userService.GetByIdAsync(Guid.Parse(User.Identity.GetUserId()));

            if (user.Role != Role.Administrator)
            {
                return BadRequest();
            }

            if (order == null)
            {
                return RedirectToAction(nameof(Delete));
            }

            try
            {
                await _orderService.Delete(id);              
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                return RedirectToAction(nameof(Delete), new { id = id, saveChangesError = true });
            }
        }
    }
}
