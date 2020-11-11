using Microsoft.Extensions.Logging;
using System.Configuration;

namespace HeapKeeper
{
    public interface IUserService
    {
        bool IsValidUser(string userName, string password);
    }

    public class UserService : IUserService
    {
        private readonly string _userName;
        private readonly string _password;

        public UserService(string username, string password)
        {
            _userName = username;
            _password = password;
        }

        public bool IsValidUser(string userName, string password)
        {
            if (string.Equals(userName , _userName)
                && string.Equals(password, _password))
            {
                return true;
            } else
            {
                return false;
            }
        }
    }
}