using GooseServerBrowserService.Contract;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace GooseServerBrowserService
{
    public class ServerBrowserController : ApiController
    {
        [HttpPost]
        public void Register(RegisterRequest request)
        {
            if (request.Checksum != request.ComputeChecksum())
                return;

            OwinContext owinContext = (OwinContext)this.Request.Properties["MS_OwinContext"];
            string ipAddress = owinContext.Request.RemoteIpAddress;

            using (var connection = CreateConnection())
            {
                connection.Open();
                bool exists = false;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT 1 FROM [Server] WHERE [Address] = @ipAddress AND [Port] = @port";
                    command.Parameters.AddWithValue("@ipAddress", ipAddress);
                    command.Parameters.AddWithValue("@port", request.Port);

                    exists = command.ExecuteScalar() != null;
                }

                if (!exists)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO [Server] ([Name], [Address], [Port], [PlayerCount], [Version], [LastUpdate]) VALUES (@name, @ipAddress, @port, @playerCount, @version, @lastUpdate)";
                        command.Parameters.AddWithValue("@name", request.ServerName);
                        command.Parameters.AddWithValue("@ipAddress", ipAddress);
                        command.Parameters.AddWithValue("@port", request.Port);
                        command.Parameters.AddWithValue("@playerCount", request.PlayerCount);
                        command.Parameters.AddWithValue("@version", request.Version);
                        command.Parameters.AddWithValue("@lastUpdate", DateTime.Now);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE [Server] SET [Name] = @name, [Address] = @ipAddress, [Port] = @port, [PlayerCount] = @playerCount, [Version] = @version, [LastUpdate] = @lastUpdate WHERE [Address] = @ipAddress AND [Port] = @port";
                        command.Parameters.AddWithValue("@name", request.ServerName);
                        command.Parameters.AddWithValue("@ipAddress", ipAddress);
                        command.Parameters.AddWithValue("@port", request.Port);
                        command.Parameters.AddWithValue("@playerCount", request.PlayerCount);
                        command.Parameters.AddWithValue("@version", request.Version);
                        command.Parameters.AddWithValue("@lastUpdate", DateTime.Now);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private SqlConnection CreateConnection()
        {
            // User Id=GooseServer;Password=password1;
            return new SqlConnection("Server=localhost;Trusted_Connection=yes;Database=GooseServerBrowser;Connection Timeout=30");
        }
    }
}
