﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using Orchard.Environment;
using Orchard.Environment.Configuration;
using Orchard.Logging;
using Orchard.Recipes.Models;
using Orchard.Setup.Services;
using Orchard.Setup.ViewModels;
using Orchard.Localization;
using Orchard.Themes;
using Orchard.UI.Notify;
using Orchard.Utility.Extensions;

namespace Orchard.Setup.Controllers {
    [ValidateInput(false), Themed]
    public class SetupController : Controller {
        private readonly IViewsBackgroundCompilation _viewsBackgroundCompilation;
        private readonly INotifier _notifier;
        private readonly ISetupService _setupService;
        private const string DefaultRecipe = "Default";


        public SetupController(
            INotifier notifier, 
            ISetupService setupService, 
            IViewsBackgroundCompilation viewsBackgroundCompilation) {
            _viewsBackgroundCompilation = viewsBackgroundCompilation;
            _notifier = notifier;
            _setupService = setupService;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        private ActionResult IndexViewResult(SetupViewModel model) {
            return View(model);
        }

        public ActionResult Index() {
            var initialSettings = _setupService.Prime();
            var recipes = OrderRecipes(_setupService.Recipes());
            string recipeDescription = null;
            if (recipes.Count > 0) {
                recipeDescription = recipes[0].Description;
            }
            
            // On the first time installation of Orchard, the user gets to the setup screen, which
            // will take a while to finish (user inputting data and the setup process itself).
            // We use this opportunity to start a background task to "pre-compile" all the known
            // views in the app folder, so that the application is more reponsive when the user
            // hits the homepage and admin screens for the first time.))
            if (StringComparer.OrdinalIgnoreCase.Equals(initialSettings.Name, ShellSettings.DefaultName)) {
                //_viewsBackgroundCompilation.Start();
            }

            //

            return IndexViewResult(new SetupViewModel {
                AdminUsername = "admin",
                Recipes = recipes,
                RecipeDescription = recipeDescription,
                SiteName = "Orchard",
                DatabaseTablePrefix = "orchard",
                DatabaseConnectionString = ConfigurationManager.ConnectionStrings["SqlAzureConnection"].ConnectionString,
                DatabaseIsPreconfigured = true,
                AdminPassword = "admin",
                ConfirmPassword = "admin"
            });
        }

        [HttpPost, ActionName("Index")]
        public ActionResult IndexPOST(SetupViewModel model) {

            HttpContext.Server.ScriptTimeout = 600;
            var recipes = OrderRecipes(_setupService.Recipes());

            model.AdminPassword = "admin";
            model.ConfirmPassword = "admin";

            //TODO: Couldn't get a custom ValidationAttribute to validate two properties
            if (!model.DatabaseOptions && string.IsNullOrEmpty(model.DatabaseConnectionString))
                ModelState.AddModelError("DatabaseConnectionString", T("A SQL connection string is required").Text);

            if (!String.IsNullOrWhiteSpace(model.ConfirmPassword) && model.AdminPassword != model.ConfirmPassword ) {
                ModelState.AddModelError("ConfirmPassword", T("Password confirmation must match").Text);
            }

            if(!model.DatabaseOptions && !String.IsNullOrWhiteSpace(model.DatabaseTablePrefix)) {
                model.DatabaseTablePrefix = model.DatabaseTablePrefix.Trim();
                if(!Char.IsLetter(model.DatabaseTablePrefix[0])) {
                    ModelState.AddModelError("DatabaseTablePrefix", T("The table prefix must begin with a letter").Text);
                }
            }
            if (model.Recipe == null) {
                if (!(recipes.Select(r => r.Name).Contains(DefaultRecipe))) {
                    ModelState.AddModelError("Recipe", T("No recipes were found in the Setup module").Text);
                }
                else {
                    model.Recipe = DefaultRecipe;
                }
            }

            if (!ModelState.IsValid)
            {
                model.Recipes = recipes;
                foreach (var recipe in recipes.Where(recipe => recipe.Name == model.Recipe))
                {
                    model.RecipeDescription = recipe.Description;
                }
                model.DatabaseIsPreconfigured = !string.IsNullOrEmpty(_setupService.Prime().DataProvider);

                //return IndexViewResult(model);
            }

            try {
                var setupContext = new SetupContext {
                    SiteName = model.SiteName,
                    AdminUsername = model.AdminUsername,
                    AdminPassword = model.AdminPassword,
                    DatabaseProvider = "SqlServer",
                    DatabaseConnectionString = model.DatabaseConnectionString,
                    DatabaseTablePrefix = model.DatabaseTablePrefix,
                    EnabledFeatures = null, // default list
                    Recipe = model.Recipe
                };

                string executionId = _setupService.Setup(setupContext);

                // First time installation if finally done. Tell the background views compilation
                // process to stop, so that it doesn't interfere with the user (asp.net compilation
                // uses a "single lock" mechanism for compiling views).
                //_viewsBackgroundCompilation.Stop();

                // redirect to the welcome page.
                return Redirect("~/");
            } catch (Exception ex) {
                Logger.Error(ex, "Setup failed");
                _notifier.Error(T("Setup failed: {0}", ex.Message));

                model.Recipes = recipes;
                foreach (var recipe in recipes.Where(recipe => recipe.Name == model.Recipe)) {
                    model.RecipeDescription = recipe.Description;
                }
                model.DatabaseIsPreconfigured = !string.IsNullOrEmpty(_setupService.Prime().DataProvider);

                return IndexViewResult(model);
            }
        }

        private static List<Recipe> OrderRecipes(IEnumerable<Recipe> recipes) {
            var recipeList = new List<Recipe>();
            var tempList = new List<Recipe>();
            foreach (var recipe in recipes) {
                if (recipe.Name == DefaultRecipe) {
                    recipeList.Add(recipe);
                }
                else {
                    tempList.Add(recipe);
                }
            }
            return recipeList.Concat(tempList).ToList();
        }
    }
}