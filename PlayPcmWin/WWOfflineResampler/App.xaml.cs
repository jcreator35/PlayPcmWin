using System.Windows;
using WWMath;

namespace WWOfflineResampler {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private void Application_Startup(object sender, StartupEventArgs e) {
            //JenkinsTraubRpoly.Test();
            //PolynomialRootFinding.Test();
            //WWPolynomial.Test();
            //NewtonsMethod.Test();
            var main = new Main();

            if (main.ParseCommandLine(e.Args)) {
                Application.Current.Shutdown();
                return;
            }
        }
    }
}
