﻿using System.Threading.Tasks;
using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAdvert.Web.Models.Accounts;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAdvert.Web.Controllers
{
  public class Accounts : Controller
  {
    private readonly SignInManager<CognitoUser> _signInManager;
    private readonly UserManager<CognitoUser> _userManager;
    private readonly CognitoUserPool _pool;

    public Accounts(
        SignInManager<CognitoUser> signInManager,
        UserManager<CognitoUser> userManager,
        CognitoUserPool pool)
    {
      _signInManager = signInManager;
      _userManager = userManager;
      _pool = pool;
    }

    public async Task<IActionResult> Signup()
    {
      var model = new SignupModel();

      return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Signup(SignupModel model)
    {
      if (ModelState.IsValid)
      {
        var user = _pool.GetUser(model.Email);
        if (user.Status != null)
        {
          ModelState.AddModelError("UserExists", "User with this email already exists");

          return View(model);
        }

        user.Attributes.Add(CognitoAttribute.Name.AttributeName, model.Email);
        var createdUser = await _userManager.CreateAsync(user, model.Password).ConfigureAwait(false);
        if (createdUser.Succeeded)
        {
          RedirectToAction("Confirm", model);
        }
      }

      return View();
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(ConfirmModel model)
    {
      return View(model);
    }

    [HttpPost]
    [ActionName("Confirm")]
    public async Task<IActionResult> ConfirmPost(ConfirmModel model)
    {
      if (ModelState.IsValid)
      {
        var user = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
        if (user == null)
        {
          ModelState.AddModelError("NotFound", "A user with the given email address was not found");

          return View(model);
        }

        var result = await (_userManager as CognitoUserManager<CognitoUser>)
            .ConfirmSignUpAsync(user, model.Code, true)
            .ConfigureAwait(false);
        if (result.Succeeded)
        {
          return RedirectToAction("Index", "Home");
        }

        foreach (var item in result.Errors)
        {
          ModelState.AddModelError(item.Code, item.Description);
        }
      }

      return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Login(LoginModel model)
    {
      return View(model);
    }

    [HttpPost]
    [ActionName("Login")]
    public async Task<IActionResult> LoginPost(LoginModel model)
    {
      if (ModelState.IsValid)
      {
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, false)
            .ConfigureAwait(false);
        if (result.Succeeded)
        {
          return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("LoginError", "Email and password do not match");
      }

      return View("Login", model);
    }
  }
}
