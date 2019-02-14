using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TcpWindowsServer
{
    public partial class TcpWindowsService : ServiceBase
    {
        private int port = Properties.Settings.Default.Port;
        EventLog eventLog;
        BackgroundWorker worker;
        private int eventId = 1;
        private void InitializeEventLog()
        {
            var source = Properties.Settings.Default.EventLogSource;
            var logName = Properties.Settings.Default.EventLogName;
            eventLog = new EventLog();
            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(
                    source, logName);
            }
            eventLog.Source = source;
            eventLog.Log = logName;
        }


        private void InitializeBackgroundWorker()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (se, ev) =>
            {
                eventLog.WriteEntry($"The service listening from port: {port}.", EventLogEntryType.Information, eventId++);
                StartTcpListener();
            };
            worker.RunWorkerCompleted += (se, ev) =>
            {
                eventLog.WriteEntry($"The service stopped.", EventLogEntryType.Information, eventId++);
            };
        }

        public TcpWindowsService()
        {
            InitializeComponent();
            InitializeEventLog();
            InitializeBackgroundWorker();
        }

        protected override void OnStart(string[] args)
        {
            var serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);


            eventLog.WriteEntry("In OnStart", EventLogEntryType.Information, eventId++);
            worker.RunWorkerAsync();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnPause()
        {
            eventLog.WriteEntry("In OnPause", EventLogEntryType.Information, eventId++);
            worker.CancelAsync();
        }
        protected override void OnStop()
        {
            eventLog.WriteEntry("In OnStop", EventLogEntryType.Information, eventId++);
            worker.CancelAsync();
        }
        protected override void OnContinue()
        {
            eventLog.WriteEntry("In OnContinue", EventLogEntryType.Information, eventId++);
            worker.RunWorkerAsync();
        }

        private void StartTcpListener()
        {
            TcpListener server = null;
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                server = new TcpListener(localAddr, port);
                server.Start();

                Byte[] bytes = new Byte[256];
                String data = null;

                while (true)
                {
                    eventLog.WriteEntry($"Waiting for a connection... ", EventLogEntryType.Information, eventId++);

                    TcpClient client = server.AcceptTcpClient();

                    eventLog.WriteEntry($"Connected!", EventLogEntryType.Information, eventId++);
                    data = null;
                    NetworkStream stream = client.GetStream();
                    int i;
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        try
                        {
                            data = Encoding.UTF8.GetString(bytes, 0, i);
                            eventLog.WriteEntry($"Received: {data}", EventLogEntryType.Information, eventId++);

                            data = data.ToUpper();

                            byte[] msg = Encoding.UTF8.GetBytes(data);
                            stream.Write(msg, 0, msg.Length);
                            eventLog.WriteEntry($"Sent: {data}", EventLogEntryType.Information, eventId++);
                        }
                        catch (Exception ex)
                        {
                            var exMessage = Encoding.UTF8.GetBytes(ex.ToString());
                            eventLog.WriteEntry($"Hata: {ex}", EventLogEntryType.Error, eventId++);
                            stream.Write(exMessage, 0, exMessage.Length);
                        }
                    }
                    client.Close();

                }
            }
            catch (SocketException e)
            {
                eventLog.WriteEntry($"SocketException: {e}", EventLogEntryType.Error, eventId++);
            }
            catch (Exception e)
            {
                eventLog.WriteEntry($"Exception: {e}", EventLogEntryType.Error, eventId++);
            }
            finally
            {
                server.Stop();
            }
        }
    }
}
