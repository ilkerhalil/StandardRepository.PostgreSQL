using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using NUnit.Framework;
using StandardRepository.Helpers;
using StandardRepository.Models;

namespace StandardRepository.PostgreSQL.IntegrationTests.Base
{
    [TestFixture]
    public class BaseRepositoryIntegrationTests
    {
        protected long CURRENT_USER_ID => 123;

        protected string GetTestDBName(string postfix = "")
        {
            var testMethodName = TestContext.CurrentContext.Test.MethodName;
            return $"test_db_{testMethodName.ToLowerInvariant()}_{postfix}";
        }

        protected EntityUtils GetEntityUtils(TypeLookup typeLookup)
        {
            return new EntityUtils(typeLookup, Assembly.GetExecutingAssembly());
        }

        protected async Task<ConnectionSettings> GetConnectionSettings(string dbName = null)
        {
            DockerClientConfiguration dockerClientConfiguration;
//            var dockerClient = new DockerClientConfiguration()
//                .CreateClient();
//            
            if (Environment.OSVersion.Platform == PlatformID.Unix ||
                Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                dockerClientConfiguration = new DockerClientConfiguration(
                    new Uri("unix:///var/run/docker.sock"));
            }
            else
            {
                dockerClientConfiguration = new DockerClientConfiguration(
                    new Uri("npipe://./pipe/docker_engine"));
            }

            var dockerClient = dockerClientConfiguration.CreateClient();
            var parameters = new CreateContainerParameters
            {
                Name = "my_sql",
                Image = "mysql:5.7",
                Env = new List<string>() {"MYSQL_ROOT_PASSWORD=pass"},
                HostConfig = new HostConfig
                {
                    Binds = new List<string>() {"D:\\backups:/backups"}, //Create volumes binding to share folder
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {"3306/tcp", new[] {new PortBinding {HostPort = "3306"}}}
                    }
                }
            };

            var container = await dockerClient.Containers.CreateContainerAsync(parameters);
            await dockerClient.Containers.StartContainerAsync(container.ID, null);

//Create backup
            var createParams = new ContainerExecCreateParameters()
            {
                Cmd = new List<string>() { "/bin/sh", "-c", "mysqldump --all-databases -uroot -ppass > backups/script.sql", }
            };

            var exec = await dockerClient.Containers.ExecCreateContainerAsync(container .ID, createParams);
            await dockerClient.Containers.StartContainerExecAsync(exec.ID);
            

            var connectionSettings = new ConnectionSettings();
            connectionSettings.DbHost = "localhost";
            connectionSettings.DbUser = "local_user";
            connectionSettings.DbPassword = "local_user+2019*";
            connectionSettings.DbPort = "5432";

            if (dbName == null)
            {
                connectionSettings.DbName = "test_db";
            }
            else
            {
                connectionSettings.DbName = dbName;
            }

            return connectionSettings;
        }

        public void Sleep()
        {
            Thread.Sleep(1234);
        }
    }
}