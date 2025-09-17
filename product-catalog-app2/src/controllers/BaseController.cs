using Microsoft.AspNetCore.Mvc;

namespace product_catalog_app.src.controllers
{
    /// <summary>
    /// Base controller class providing common functionality for all controllers
    /// Implements DRY principle by centralizing common error handling, logging, and UI operations
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected readonly ILogger _logger;

        protected BaseController(ILogger logger)
        {
            _logger = logger;
        }

        #region Common Error Handling

        /// <summary>
        /// Handles standard form exceptions with consistent error messaging and logging
        /// </summary>
        protected virtual IActionResult HandleFormException(Exception ex, string userErrorMessage, object? model = null, string? viewName = null)
        {
            _logger.LogError(ex, userErrorMessage);
            TempData["Error"] = userErrorMessage;
            
            if (model != null)
            {
                return View(viewName, model);
            }
            
            return View(viewName);
        }

        /// <summary>
        /// Handles async form exceptions with consistent error messaging and logging
        /// </summary>
        protected virtual Task<IActionResult> HandleFormExceptionAsync(Exception ex, string userErrorMessage, object? model = null, string? viewName = null)
        {
            _logger.LogError(ex, userErrorMessage);
            TempData["Error"] = userErrorMessage;
            
            IActionResult result;
            if (model != null)
            {
                result = View(viewName, model);
            }
            else
            {
                result = View(viewName);
            }
            
            return Task.FromResult(result);
        }

        /// <summary>
        /// Handles AJAX/API exceptions with JSON response
        /// </summary>
        protected virtual IActionResult HandleAjaxException(Exception ex, string userErrorMessage)
        {
            _logger.LogError(ex, userErrorMessage);
            return Json(new { success = false, message = userErrorMessage });
        }

        #endregion

        #region Common Success Messages

        /// <summary>
        /// Sets success message in TempData for display after redirect
        /// </summary>
        protected virtual void SetSuccessMessage(string message)
        {
            TempData["Success"] = message;
        }

        /// <summary>
        /// Sets error message in TempData for display after redirect
        /// </summary>
        protected virtual void SetErrorMessage(string message)
        {
            TempData["Error"] = message;
        }

        /// <summary>
        /// Sets warning message in TempData for display after redirect
        /// </summary>
        protected virtual void SetWarningMessage(string message)
        {
            TempData["Warning"] = message;
        }

        /// <summary>
        /// Sets info message in TempData for display after redirect
        /// </summary>
        protected virtual void SetInfoMessage(string message)
        {
            TempData["Info"] = message;
        }

        #endregion

        #region Common Validation

        /// <summary>
        /// Validates if model state is valid and optionally adds custom validation errors
        /// </summary>
        protected virtual bool ValidateModel(object model, Dictionary<string, string>? customValidationErrors = null)
        {
            if (customValidationErrors != null)
            {
                foreach (var error in customValidationErrors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
            }

            return ModelState.IsValid;
        }

        /// <summary>
        /// Clears validation errors for specified fields (useful for optional fields)
        /// </summary>
        protected virtual void ClearValidationErrors(params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                ModelState.Remove(fieldName);
            }
        }

        #endregion

        #region Common Redirects

        /// <summary>
        /// Redirects to action with success message
        /// </summary>
        protected virtual IActionResult RedirectWithSuccess(string actionName, string successMessage, string? controllerName = null, object? routeValues = null)
        {
            SetSuccessMessage(successMessage);
            return RedirectToAction(actionName, controllerName, routeValues);
        }

        /// <summary>
        /// Redirects to action with error message
        /// </summary>
        protected virtual IActionResult RedirectWithError(string actionName, string errorMessage, string? controllerName = null, object? routeValues = null)
        {
            SetErrorMessage(errorMessage);
            return RedirectToAction(actionName, controllerName, routeValues);
        }

        #endregion

        #region Common Logging

        /// <summary>
        /// Logs information with standardized format
        /// </summary>
        protected virtual void LogInfo(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        /// <summary>
        /// Logs warning with standardized format
        /// </summary>
        protected virtual void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        /// <summary>
        /// Logs error with standardized format
        /// </summary>
        protected virtual void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, message, args);
        }

        #endregion

        #region Common AJAX Responses

        /// <summary>
        /// Returns successful JSON response
        /// </summary>
        protected virtual IActionResult JsonSuccess(object? data = null, string? message = null)
        {
            var response = new { success = true };
            
            if (data != null && message != null)
            {
                return Json(new { success = true, data, message });
            }
            else if (data != null)
            {
                return Json(new { success = true, data });
            }
            else if (message != null)
            {
                return Json(new { success = true, message });
            }
            
            return Json(response);
        }

        /// <summary>
        /// Returns error JSON response
        /// </summary>
        protected virtual IActionResult JsonError(string message, object? data = null)
        {
            if (data != null)
            {
                return Json(new { success = false, message, data });
            }
            
            return Json(new { success = false, message });
        }

        #endregion
    }

    /// <summary>
    /// Common result class for validation operations
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public Dictionary<string, string> FieldErrors { get; set; } = new Dictionary<string, string>();
    }
}
