using ComputerUtils.CommandLine;
using ComputerUtils.Logging;
using ComputerUtils.QR;
using ComputerUtils.RandomExtensions;
using ComputerUtils.Updating;
using ComputerUtils.Webserver;
using System.Reflection;

namespace Cards_against_humanity
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.displayLogInConsole = true;
            Logger.saveOutputInVariable = true;
            CommandLineCommandContainer cla = new CommandLineCommandContainer(args);
            cla.AddCommandLineArgument(new List<string> { "--workingdir" }, false, "Sets the working Directory for CAH", "directory", "");
            cla.AddCommandLineArgument(new List<string> { "update", "--update", "-U" }, true, "Starts in update mode (use with caution. It's best to let it do on it's own)");
            cla.AddCommandLineArgument(new List<string> { "--displayMasterToken", "-dmt" }, true, "Outputs the master token without starting the server");
            if (cla.HasArgument("help"))
            {
                cla.ShowHelp();
                return;
            }

            string workingDir = cla.GetValue("--workingdir");
            if (workingDir.EndsWith("\"")) workingDir = workingDir.Substring(0, workingDir.Length - 1);

            CAHEnvironment.workingDir = workingDir;
            CAHEnvironment.AddVariablesDependentOnVariablesAndFixAllOtherVariables();
            if (cla.HasArgument("update"))
            {
                Updater.UpdateNetApp(Path.GetFileName(Assembly.GetExecutingAssembly().Location), CAHEnvironment.workingDir);
            }
            CAHEnvironment.config = Config.LoadConfig();
            if (CAHEnvironment.config.masterToken == "") CAHEnvironment.config.masterToken = RandomExtension.CreateToken();
            CAHEnvironment.config.Save();
            //Logger.SetLogFile(workingDir + "Log.log");

            if (cla.HasArgument("-dmt"))
            {
                QRCodeGeneratorWrapper.Display(CAHEnvironment.config.masterToken);
                return;
            }
            Server s = new Server();
            s.StartServer();
        }
    }
}