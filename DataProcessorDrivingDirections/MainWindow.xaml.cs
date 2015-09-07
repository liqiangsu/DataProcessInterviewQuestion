using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Net;
using Path = System.IO.Path;

namespace DataProcessorDrivingDirections
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker googleWorker;
        public MainWindow()
        {
            InitializeComponent();
            googleWorker = new BackgroundWorker();
            googleWorker.DoWork += DoWorkAskGoogle;
            googleWorker.RunWorkerCompleted += DoWorkAskGoogleComplete;
        }

        private void sumbitButton_Click(object sender, RoutedEventArgs e)
        {
            //clean last search
            errorLabel.Content = "";

            string origin = originTextBox.Text;
            string destination = destinationTextBox.Text;
            bool isAlthernative = althernativeCheckBox.IsChecked ?? false;

            if (CheckInputs(origin, destination)) return;

            string requestUrl = "https://maps.googleapis.com/maps/api/directions/xml?" +
                     "&origin="+ origin +
                     "&destination=" + destination + 
                     (isAlthernative ? "&alternatives=true" : "");

            googleWorker.RunWorkerAsync(requestUrl);
        }

        private void DoWorkAskGoogle(Object sender, DoWorkEventArgs e)
        {
            try
            {
                var html = AskGoogle(e.Argument as string);
                WriteHtmlFile(html);
                e.Result = "success";
            }
            catch (Exception er)
            {
                e.Result = er.Message;
            }
        }

        private void DoWorkAskGoogleComplete(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e )
        {
            if (e.Result as string != "success")
            {
                errorLabel.Content = e.Result;
            }
        }

        private static bool CheckInputs(string origin, string destination)
        {
            if (origin == "" || destination == "")
            {
                MessageBox.Show("Origin and destination should no be empty, please try again");
                return true;
            }
            return false;
        }

        //sending request to google api
        /**
         * throw <exception>many </exception>
         * <returns>html string</returns>
         */
        private string AskGoogle(string requestUrl)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                //cast check, should be fine without checking, just in case,
                if (response == null) return "";
                //check http state
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw  new Exception(response.StatusCode + " : " + response.StatusDescription);
                }

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(response.GetResponseStream());
                return GenreateHTML(xmlDoc);
            }
            catch (Exception)
            {
                throw new Exception( "There is some problem talking with google");
            }
        }

        /** building html string that containes each route and each steps in the route
         * <param>A xml document parsed from google direction api</param>
         **/
        private string GenreateHTML(XmlDocument xml)
        {

            if(xml.SelectSingleNode("//status").InnerText == "ZERO_RESULTS" || xml.SelectSingleNode("//status").InnerText == "NOT_FOUND")
            {
                throw  new Exception("Google Return no result for this seach try other input instead");
            }
            var html = new StringBuilder();
            html.Append("<!DOCTYPE html>");
            html.Append("<html>");
            foreach (XmlNode route in xml.SelectNodes("//route"))
            {
                BuildRoute(route, html);
            }
            html.Append("</html>");
            return html.ToString();
        }

        private static void BuildRoute(XmlNode route, StringBuilder html)
        {
            var h2 = route.SelectSingleNode("./summary").InnerText;
            html.Append("<h2>" + h2 + "</h2>");
            html.Append("<table>");
            foreach (XmlNode leg in route.SelectNodes("./leg"))
            {
                BuildSteps(html, leg);
            }
            route.SelectNodes("/leg");
            html.Append("</table>");
        }

        private static void BuildSteps(StringBuilder html, XmlNode leg)
        {
            html.Append("<tr>");
            var legStr = "Leg: " + leg.SelectSingleNode("./duration/text").InnerText;
            html.Append("<td><b>" + legStr + "</b></td>");
            html.Append("<td><table>");
            foreach (XmlNode step in leg.SelectNodes("./step"))
            {
                html.Append("<tr>");
                html.Append("<td>" + step.SelectSingleNode("./html_instructions").InnerText + "</td>");
                html.Append("<td>" + step.SelectSingleNode("./duration/text").InnerText + "</td>");
                html.Append("</tr>");
            }
            html.Append("</table></td>");

            html.Append("</tr>");
        }

        private void WriteHtmlFile(string html)
        {
            var tempPath = Path.GetTempPath() + "reuslt.html";
            using (var file = new StreamWriter(tempPath))
            {
                file.Write(html);
            }
            Process.Start(tempPath);
        }
    }
}
