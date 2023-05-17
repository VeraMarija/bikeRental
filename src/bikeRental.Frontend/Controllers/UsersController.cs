﻿using bikeRental.Application.Models.Station;
using bikeRental.Application;
using bikeRental.Application.Models.User;
using bikeRental.Application.Services;
using bikeRental.Application.Services.Impl;
using bikeRental.Core.Entities;
using bikeRental.Core.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace bikeRental.Frontend.Controllers;
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public UsersController(IMapper mapper, IUserService userService, UserManager<ApplicationUser> userManager)
    {
        _mapper = mapper;
        _userService = userService;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Roles = ("Administrator"))]
    public async Task<IActionResult> Index(string currentCategory, string currentFilter, string searchString, int? pageNumber)
    {
        if (searchString != null)
        {
            pageNumber = 1;
        }
        else
        {
            searchString = currentFilter;
        }

        ViewData["CurrentCategory"] = currentCategory;
        ViewData["CurrentFilter"] = searchString;
        int pageSize = 5;
        var users = await _userService.GetAllUsers();


        return View("/Pages/Users/Index.cshtml", PaginatedList<UserModel>.Create(users, pageNumber ?? 1, pageSize));
    }

    [HttpGet]
    [Authorize(Roles = ("Administrator"))]
    public IActionResult Create()
    { 
        return View("/Pages/Users/Create.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    [Bind("FirstName,LastName,Email,Password,ConfirmPassword,Role")] RegisterUserModel userModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                await _userService.AddAsync(userModel);
            }
        }
        catch (DbUpdateException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ModelState.AddModelError("", "Unable to save changes. " + ex);
        }
        return RedirectToAction(nameof(Index));
    }

        [HttpGet]
    [Authorize(Roles = ("Administrator"))]
    public async Task<IActionResult> Delete(Guid? id, bool? saveChangesError = false)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userService.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        if (saveChangesError.GetValueOrDefault())
        {
            ViewData["ErrorMessage"] =
                "Delete failed. Try again, and if the problem persists " +
                "see your system administrator.";
        }

        return View("/Pages/Users/Delete.cshtml", user);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = ("Administrator"))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user == null)
        {
            return RedirectToAction(nameof(Delete));
        }

        try
        {

            await _userService.DeleteAsync(id);
            //return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            //Log the error (uncomment ex variable name and write a log.)
            return RedirectToAction(nameof(Delete), new { id = id, saveChangesError = true });
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ("Administrator"))]
    public async Task<IActionResult> Edit([Bind("Id,FirstName,LastName,Email,Role")] Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userModel = await _userService.GetByIdAsync(id);
        
        if (userModel == null)
        {
            return NotFound();
        }

        var editUserModel = _mapper.Map<EditUserModel>(userModel);

        return View("/Pages/Users/Edit.cshtml", editUserModel);
    }

    [HttpPost, ActionName("Edit")]
    [Authorize(Roles = ("Administrator"))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost([Bind("Id,FirstName,LastName,Email,Role")] EditUserModel userModel)
    {
        if (userModel == null)
        {
            return NotFound();
        }

        try
        {
            await _userService.UpdateAsync(userModel);
        }
        catch (DbUpdateException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ModelState.AddModelError("", "Unable to save changes. " + ex);
        }

        return RedirectToAction(nameof(Index));
    }
}