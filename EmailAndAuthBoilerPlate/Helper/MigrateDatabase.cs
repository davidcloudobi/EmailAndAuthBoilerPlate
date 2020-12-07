using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Web;

namespace EmailAndAuthBoilerPlate.Helper
{/// <summary>
/// 
/// </summary>
    public static class MigrateDatabase
        {
            /// <summary>
        /// 
        /// </summary>
        /// <param name="webHost"></param>
        /// <returns></returns>
            public static IHost EMigrateDatabase(this IHost webHost)
            {
                using (var scope = webHost.Services.CreateScope())
                {
                    var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
                    var services = scope.ServiceProvider;

                    try
                    {
                        var db = services.GetRequiredService<ApplicationDbContext>();
                        db.Database.Migrate();
                        logger.Debug("Migration applied sucessfully");

                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "An error occurred while migrating the database.");
                    }

                }

                return webHost;
            }
        }
    }
