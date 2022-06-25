using ComputerUtils.Encryption;
using ComputerUtils.RandomExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cards_against_humanity
{
    public class UserSystem
    {
        public static string HashPassword(string password, string salt)
        {
            return Hasher.GetSHA256OfString(password + salt);
        }

        public static string GetSalt()
        {
            return RandomExtension.CreateRandom(4, "0123456789ABCDEF");
        }

        public static LoginResponse ProcessLogin(LoginRequest request)
        {
            LoginResponse response = new LoginResponse();
            if (!MongoDBInteractor.DoesUserExist(request.username))
            {
                response.message = "User does not exist.";
                return response;
            }
            UserInfo user = MongoDBInteractor.GetUserInfo(request.username);
            if (user.passwordSHA256 != HashPassword(request.password, user.passwordSalt))
            {
                response.message = "Wrong password";
                return response;
            }
            string token = RandomExtension.CreateToken();
            user.currentTokenSHA256 = Hasher.GetSHA256OfString(token);
            MongoDBInteractor.UpdateUser(user);
            response.token = token;
            response.success = true;
            response.message = "Logged in and invalidated old session";
            return response;
        }

        public static LoginResponse ProcessCreateUser(LoginRequest request)
        {
            LoginResponse response = new LoginResponse();
            if(MongoDBInteractor.DoesUserExist(request.username))
            {
                response.message = "User already exists. Please choose another name.";
                return response;
            }
            string salt = GetSalt();
            string token = RandomExtension.CreateToken();
            MongoDBInteractor.AddUser(new UserInfo
            {
                passwordSalt = salt,
                passwordSHA256 = HashPassword(request.password, salt),
                nickname = request.username,
                currentTokenSHA256 = Hasher.GetSHA256OfString(token)
            });
            response.token = token;
            response.success = true;
            response.message = "User created";
            return response;
        }
    }
}
