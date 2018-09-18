using System;
using System.Threading.Tasks;

namespace Opc.Ua.Client.Controls
{
    public class ConnectCtrl
    {
        #region Private Fields
        private ApplicationConfiguration m_configuration;
        private Session m_session;
        private int m_reconnectPeriod = 10;
        private int m_discoverTimeout = 5000;
        private SessionReconnectHandler m_reconnectHandler;
        private CertificateValidationEventHandler m_CertificateValidation;
        private EventHandler m_ReconnectComplete;
        private EventHandler m_ReconnectStarting;
        private EventHandler m_KeepAliveComplete;
        private EventHandler m_ConnectComplete;
        private string serverUrl;

        #endregion

        public ConnectCtrl(ApplicationConfiguration configuration, string serverUrl)
        {
            m_configuration = configuration;
            // determine the URL that was selected.
            this.serverUrl = serverUrl;

        }

        /// <summary>
        /// The client application configuration.
        /// </summary>
        public ApplicationConfiguration Configuration
        {
            get { return m_configuration; }

            set
            {
                if (!Object.ReferenceEquals(m_configuration, value))
                {
                    if (m_configuration != null)
                    {
                        m_configuration.CertificateValidator.CertificateValidation -= m_CertificateValidation;
                    }

                    m_configuration = value;

                    if (m_configuration != null)
                    {
                        m_configuration.CertificateValidator.CertificateValidation += m_CertificateValidation;
                    }
                }
            }
        }

        /// <summary>
        /// The currently active session. 
        /// </summary>
        public Session Session
        {
            get { return m_session; }
        }

        /// <summary>
        /// Gets or sets a flag indicating that the domain checks should be ignored when connecting.
        /// </summary>
        public bool DisableDomainCheck { get; set; }

        /// <summary>
        /// The name of the session to create.
        /// </summary>
        public string SessionName { get; set; }

        /// <summary>
        /// The user identity to use when creating the session.
        /// </summary>
        public IUserIdentity UserIdentity { get; set; }

        /// <summary>
        /// The locales to use when creating the session.
        /// </summary>
        public string[] PreferredLocales { get; set; }

        /// <summary>
        /// Creates a new session.
        /// </summary>
        /// <returns>The new session object.</returns>
        public async Task<Session> Connect()
        {
            // disconnect from existing session.
            //Disconnect();
          
            if (m_configuration == null)
            {
                throw new ArgumentNullException("m_configuration");
            }

            // TODO 
            bool useSecurity = false;

            // select the best endpoint.
            EndpointDescription endpointDescription = CoreClientUtils.SelectEndpoint(serverUrl, useSecurity, m_discoverTimeout);

            EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(m_configuration);
            ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

            m_session = await Session.Create(
                m_configuration,
                endpoint,
                false,
                !DisableDomainCheck,
                (String.IsNullOrEmpty(SessionName)) ? m_configuration.ApplicationName : SessionName,
                60000,
                UserIdentity,
                PreferredLocales);

            // set up keep alive callback.
            m_session.KeepAlive += new KeepAliveEventHandler(Session_KeepAlive);

            // raise an event.
            DoConnectComplete(null);

            // return the new session.
            return m_session;
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            //UpdateStatus(false, DateTime.UtcNow, "Disconnected");

            // stop any reconnect operation.
            if (m_reconnectHandler != null)
            {
                m_reconnectHandler.Dispose();
                m_reconnectHandler = null;
            }

            // disconnect any existing session.
            if (m_session != null)
            {
                m_session.Close(10000);
                m_session = null;
            }

            // raise an event.
            DoConnectComplete(null);
        }

        #region Private Methods
        /// <summary>
        /// Handles a keep alive event from a session.
        /// </summary>
        private void Session_KeepAlive(Session session, KeepAliveEventArgs e)
        {
            //if (this.InvokeRequired)
            //{
            //    this.BeginInvoke(new KeepAliveEventHandler(Session_KeepAlive), session, e);
            //    return;
            //}

            try
            {
                // check for events from discarded sessions.
                if (!Object.ReferenceEquals(session, m_session))
                {
                    return;
                }

                // start reconnect sequence on communication error.
                if (ServiceResult.IsBad(e.Status))
                {
                    if (m_reconnectPeriod <= 0)
                    {
                        Console.WriteLine("Communication Error ({0})", e.Status);
                        return;
                    }

                    Console.WriteLine(e.CurrentTime.ToString(), "Reconnecting in {0}s", m_reconnectPeriod);

                    if (m_reconnectHandler == null)
                    {
                        if (m_ReconnectStarting != null)
                        {
                            m_ReconnectStarting(this, e);
                        }

                        m_reconnectHandler = new SessionReconnectHandler();
                        m_reconnectHandler.BeginReconnect(m_session, m_reconnectPeriod * 1000, Server_ReconnectComplete);
                    }

                    return;
                }

                // update status.
                Console.WriteLine(e.CurrentTime.ToString(), "Connected [{0}]", session.Endpoint.EndpointUrl);

                // raise any additional notifications.
                if (m_KeepAliveComplete != null)
                {
                    m_KeepAliveComplete(this, e);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        /// Handles a reconnect event complete from the reconnect handler.
        /// </summary>
        private void Server_ReconnectComplete(object sender, EventArgs e)
        {
            //if (this.InvokeRequired)
            //{
            //    this.BeginInvoke(new EventHandler(Server_ReconnectComplete), sender, e);
            //    return;
            //}

            try
            {
                // ignore callbacks from discarded objects.
                if (!Object.ReferenceEquals(sender, m_reconnectHandler))
                {
                    return;
                }

                m_session = m_reconnectHandler.Session;
                m_reconnectHandler.Dispose();
                m_reconnectHandler = null;

                // raise any additional notifications.
                if (m_ReconnectComplete != null)
                {
                    m_ReconnectComplete(this, e);
                }
            }
            catch (Exception exception)
            {
                Console.Write(exception);
            }
        }

        /// <summary>
        /// Raises the connect complete event on the main GUI thread.
        /// </summary>
        private void DoConnectComplete(object state)
        {
            if (m_ConnectComplete != null)
            {
                //if (this.InvokeRequired)
                //{
                //    this.BeginInvoke(new System.Threading.WaitCallback(DoConnectComplete), state);
                //    return;
                //}

                m_ConnectComplete(this, null);
            }
        }
        #endregion
    }
}
