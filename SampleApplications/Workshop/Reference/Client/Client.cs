using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Quickstarts.ReferenceClient
{
    public class OPCClient
    {
        #region Private Fields
        private Session m_session;
        private bool m_connectedOnce;

        private ConnectCtrl ConnectCTRL;

        ReferenceDescriptionCollection tanksRef = new ReferenceDescriptionCollection();
        private Subscription m_subscription;

        #endregion

        public OPCClient(ApplicationConfiguration configuration, string serverUrl)
        {
            ConnectCTRL = new ConnectCtrl(configuration, serverUrl);
        }

        public void Run()
        {
            this.Server_ConnectMI_Click();
            this.Server_ConnectComplete();
        }

        #region Event Handlers
        /// <summary>
        /// Connects to a server.
        /// </summary>
        private async void Server_ConnectMI_Click()
        {
            try
            {
                await ConnectCTRL.Connect();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        /// Updates the application after connecting to or disconnecting from the server.
        /// </summary>
        private void Server_ConnectComplete()
        {
            try
            {
                m_session = ConnectCTRL.Session;
                if(m_session == null)
                {
                    throw new Exception("m_session is null");
                }

                // set a suitable initial state.
                if (m_session != null && !m_connectedOnce)
                {
                    m_connectedOnce = true;
                } 

                GetTanks();
                AddSubscription();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
        #endregion

        /// <summary>
        /// Disconnects from the current session.
        /// </summary>
        public void Stop()
        {
            try
            {
                m_subscription.Delete(true);
                m_subscription.Dispose();
                m_subscription = null;
                ConnectCTRL.Disconnect();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        // Get all nodes excluding Server Node in the references Collection
        private void GetTanks()
        {
            BrowseDescription nodeToBrowse = new BrowseDescription();

            nodeToBrowse.NodeId = Opc.Ua.ObjectIds.ObjectsFolder;
            nodeToBrowse.BrowseDirection = BrowseDirection.Forward;
            nodeToBrowse.ReferenceTypeId = Opc.Ua.ReferenceTypeIds.HierarchicalReferences;
            nodeToBrowse.IncludeSubtypes = true;
            nodeToBrowse.NodeClassMask = (uint)(NodeClass.Object);
            nodeToBrowse.ResultMask = (uint)(BrowseResultMask.All);

            ReferenceDescriptionCollection references = ClientUtils.Browse(
                m_session,
                nodeToBrowse,
                false);

            if (references != null)
            {
                for (int ii = 0; ii < references.Count; ii++)
                {
                    // do not add Server node
                    if (!references[ii].BrowseName.Name.Equals("Server"))
                        tanksRef.Add(references[ii]);
                }
            }
        }

        private void AddSubscription()
        {
            try
            {
                if (m_session == null)
                {
                    return;
                }

                if (m_subscription != null)
                {
                    m_session.RemoveSubscription(m_subscription);
                    m_subscription = null;
                }

                m_subscription = new Subscription();

                m_subscription.PublishingEnabled = true;
                m_subscription.PublishingInterval = 5000;
                m_subscription.Priority = 1;
                m_subscription.KeepAliveCount = 10;
                m_subscription.LifetimeCount = 20;
                m_subscription.MaxNotificationsPerPublish = 5000;

                m_session.AddSubscription(m_subscription);
                m_subscription.Create();

                for (int ii = 0; ii < tanksRef.Count; ii++)
                {
                    // filter the node (should be the tank)
                    if (tanksRef[ii].BrowseName.Name.Contains(TankDataTypes.tanks))
                    {
                        BrowseDescription n = new BrowseDescription();
                        n.NodeId = (NodeId)tanksRef[ii].NodeId;
                        n.BrowseDirection = BrowseDirection.Forward;
                        n.IncludeSubtypes = true;
                        n.NodeClassMask = (uint)(NodeClass.Variable);
                        n.ResultMask = (uint)(BrowseResultMask.All);

                        ReferenceDescriptionCollection props = ClientUtils.Browse(
                            m_session,
                            n,
                            false);

                        if (props.Count > 0)
                        {
                            for (int jj = 0; jj < props.Count; jj++)
                            {
                                // filter the Properties of the tank
                                if (TankDataTypes.containsProp(props[jj].DisplayName.Text))
                                {
                                    MonitoredItem monitoredItem = new MonitoredItem();
                                    monitoredItem.StartNodeId = (NodeId)props[jj].NodeId;
                                    monitoredItem.DisplayName = props[jj].BrowseName.Name;
                                    monitoredItem.AttributeId = Attributes.Value;
                                    monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                                    m_subscription.AddItem(monitoredItem);
                                }
                            }
                        }
                    }

                }

                m_subscription.ApplyChanges();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        void monitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            //if (this.InvokeRequired)
            //{
            //    this.BeginInvoke(new MonitoredItemNotificationEventHandler(monitoredItem_Notification), monitoredItem, e);
            //    return;
            //}

            try
            {
                MonitoredItemNotification notification = e.NotificationValue as MonitoredItemNotification;

                if (notification == null)
                {
                    return;
                }

                Console.WriteLine(monitoredItem.DisplayName + ": " + notification.Value.WrappedValue.ToString());
                WriteToFile(monitoredItem.DisplayName + ": " + notification.Value.WrappedValue.ToString());
                //using (EventLog eventLog = new EventLog("Monitored Item"))
                //{
                //    eventLog.Source = "OPC_Service Monitored Item";
                //    eventLog.WriteEntry(monitoredItem.DisplayName + ": " + notification.Value.WrappedValue.ToString(), EventLogEntryType.Information, 101, 1);
                //}
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\Tags_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
