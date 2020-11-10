using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace InboxManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> uniqueSenders = new List<string>();
        List<Google.Apis.Gmail.v1.Data.Message> messages = new List<Google.Apis.Gmail.v1.Data.Message>();
        List<string> sendersToDelete = new List<string>();
        Dictionary<string, string> messageInfo = new Dictionary<string, string>();

        //strings
        string email = "cesaracer@gmail.com";
        static string[] Scopes = { GmailService.Scope.MailGoogleCom };

        //Objects
        GmailService service;

        public string ApplicationName { get; private set; }

        public MainWindow()
        {
            InitialApiCall();
            InitializeComponent();
        }

        private void Reset()
        {
            lstSenders.Items.Clear();
            uniqueSenders.Clear();
            messageInfo.Clear();
            sendersToDelete.Clear();
        }

        private void Populate()
        {
            btnRetrieve.IsEnabled = false;
            messages = ListMessages(service, email, "");
            GetAllMessages();
            txtSenders.Text = uniqueSenders.Count.ToString();
            txtEmails.Text = messages.Count.ToString();
            for (int i = 0; i < uniqueSenders.Count; i++)
            {
                CheckBox check = new CheckBox();
                check.Content = uniqueSenders[i].ToString();
                lstSenders.Items.Add(check);
            }
            btnRetrieve.IsEnabled = false;
        }
        private void GetAllMessages()
        {
            for (int i = 0; i < messages.Count; i++)
            {
                var message = GetMessage(service, email, messages[i].Id);
                for (int x = 0; x < message.Payload.Headers.Count; x++)
                {
                    if (message.Payload.Headers[x].Name == "From")
                    {
                        messageInfo.Add(message.Id.ToString(), message.Payload.Headers[x].Value.ToString());
                        if (!uniqueSenders.Contains(message.Payload.Headers[x].Value))
                            uniqueSenders.Add(message.Payload.Headers[x].Value);
                        break;
                    }
                }
            }
        }

        private void CreateDeletionList()
        {
            List<CheckBox> senders = new List<CheckBox>();
            foreach(CheckBox item in lstSenders.Items)
            {
                if (item.IsChecked == true)
                {
                    senders.Add(item);
                }
            }

            for (int i = 0; i < senders.Count; i++)
            {
                foreach (var item in messageInfo)
                {
                    if (item.Value == senders[i].Content.ToString())
                    {
                        sendersToDelete.Add(item.Key);
                    }
                }
            }
        }

        private void InitialApiCall()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("config.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create Gmail API service.
            service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        private void DeleteEmails()
        {
            for (int i = 0; i < sendersToDelete.Count; i++)
            {
                service.Users.Messages.Trash(email, sendersToDelete[i]).Execute(); 
            }
        }

        private List<Google.Apis.Gmail.v1.Data.Message> ListMessages(GmailService service, String userId, String query)
        {
            List<Google.Apis.Gmail.v1.Data.Message> result = new List<Google.Apis.Gmail.v1.Data.Message>();
            UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List(userId);
            request.Q = query;

            do
            {
                try
                {
                    ListMessagesResponse response = request.Execute();
                    result.AddRange(response.Messages);
                    request.PageToken = response.NextPageToken;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            } while (!String.IsNullOrEmpty(request.PageToken));

            return result;
        }

        private Google.Apis.Gmail.v1.Data.Message GetMessage(GmailService service, String userId, String messageId)
        {
            try
            {
                return service.Users.Messages.Get(userId, messageId).Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }

            return null;
        }

        private void btnRetrieve_Click(object sender, RoutedEventArgs e)
        {
            Populate();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            btnRetrieve.IsEnabled = true;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lstSenders.Items.Count == 0)
                return;
            else
            {
                btnDelete.IsEnabled = false;
                CreateDeletionList();
                DeleteEmails();
                Reset();
                Populate();

                btnDelete.IsEnabled = true;
            }
        }
    }
}
