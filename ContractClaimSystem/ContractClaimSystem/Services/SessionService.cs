using ContractClaimSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
namespace ContractClaimSystem.Services
{
    public class SessionService
    {
       
            private readonly IHttpContextAccessor _httpContextAccessor;

            public SessionService(IHttpContextAccessor httpContextAccessor)
            {
                _httpContextAccessor = httpContextAccessor;
            }

            public void SetUserSession(User user)
            {
                _httpContextAccessor.HttpContext.Session.SetString("UserId", user.UserId.ToString());
                _httpContextAccessor.HttpContext.Session.SetString("UserEmail", user.Email);
                _httpContextAccessor.HttpContext.Session.SetString("UserRole", user.Role);
                _httpContextAccessor.HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
                _httpContextAccessor.HttpContext.Session.SetString("SessionId", Guid.NewGuid().ToString());
                _httpContextAccessor.HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString());
            }

            public bool IsUserLoggedIn()
            {
                return !string.IsNullOrEmpty(_httpContextAccessor.HttpContext.Session.GetString("UserId"));
            }

            public string GetUserRole()
            {
                return _httpContextAccessor.HttpContext.Session.GetString("UserRole");
            }

            public int GetUserId()
            {
                var userId = _httpContextAccessor.HttpContext.Session.GetString("UserId");
                return int.TryParse(userId, out int id) ? id : 0;
            }

            // ADD THIS MISSING METHOD:
            public string GetUserName()
            {
                return _httpContextAccessor.HttpContext.Session.GetString("UserName");
            }

            public void ClearSession()
            {
                _httpContextAccessor.HttpContext.Session.Clear();
            }

            public bool IsSessionValid()
            {
                if (!IsUserLoggedIn()) return false;

                var loginTimeStr = _httpContextAccessor.HttpContext.Session.GetString("LoginTime");
                if (DateTime.TryParse(loginTimeStr, out DateTime loginTime))
                {
                    return DateTime.Now.Subtract(loginTime).TotalMinutes < 30;
                }
                return false;
            }

        }
    }

