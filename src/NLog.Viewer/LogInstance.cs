// 
// Copyright (c) 2004,2005 Jaroslaw Kowalski <jkowalski@users.sourceforge.net>
// 
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of the Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using System.Threading;
using System.Windows.Forms;
using System.Text;
using System.IO;
using System.Collections;
using System.Drawing;

using NLog.Viewer.Receivers;
using NLog.Viewer.Configuration;
using NLog.Viewer.UI;

namespace NLog.Viewer
{
	public class LogInstance
	{
        private LogInstanceConfiguration _config;
        private LogEventReceiver _receiver;
        private ListView _listView;
        private TreeView _treeView;

		public LogInstance(LogInstanceConfiguration config)
		{
            _config = config;
            _receiver = LogReceiverFactory.CreateLogReceiver(config.ReceiverType, config.ReceiverParameters);
            _receiver.Connect(this);
		}

        public LogInstanceConfiguration Config
        {
            get { return _config; }
        }

        public TabPage CreateTab(MainForm form)
        {
            TabPage page = new TabPage(Config.Name);
            page.ImageIndex = 1;

            TreeView treeView = new TreeView();
            treeView.Nodes.Add(new TreeNode("Loggers"));
            treeView.Dock = DockStyle.Left;
            treeView.ContextMenu = form.treeContextMenu;

            Splitter splitter = new Splitter();
            splitter.Dock = DockStyle.Left;

            ListView listView = new ListView();
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.Font = new Font("Tahoma", 8);

            listView.Columns.Add("Time", 120, HorizontalAlignment.Left);
            listView.Columns.Add("Logger", 200, HorizontalAlignment.Left);
            listView.Columns.Add("Level", 50, HorizontalAlignment.Left);
            listView.Columns.Add("Text", 300, HorizontalAlignment.Left);

            _listView = listView;
            _treeView = treeView;

            page.Controls.Add(listView);
            page.Controls.Add(splitter);
            page.Controls.Add(treeView);

            return page;
        }

        public void Start()
        {
            _receiver.Start();
        }

        public void Stop()
        {
            _receiver.Stop();
        }

        public bool IsRunning
        {
            get { return _receiver.IsRunning; }
        }

        private Hashtable _logger2node = new Hashtable();

        private void LoggerToNode(string logger, out TreeNode treeNode, out LoggerConfig loggerConfigInfo)
        {
            object o = _logger2node[logger];
            if (o != null)
            {
                treeNode = (TreeNode)o;
                loggerConfigInfo = (LoggerConfig)treeNode.Tag;
                return;
            }

            TreeNode parentNode;
            LoggerConfig parentLoggerConfigInfo;

            string baseName;
            int rightmostDot = logger.LastIndexOf('.');
            if (rightmostDot < 0)
            {
                parentNode = _treeView.Nodes[0];
                baseName = logger;
                parentLoggerConfigInfo = Config.GetLoggerConfig("");
            }
            else
            {
                string parentLoggerName = logger.Substring(0, rightmostDot);
                baseName = logger.Substring(rightmostDot + 1);
                LoggerToNode(parentLoggerName, out parentNode, out parentLoggerConfigInfo);
            }

            LoggerConfig lci = Config.GetLoggerConfig(logger);

            TreeNode newNode = new TreeNode(baseName);
            _logger2node[logger] = newNode;
            newNode.Tag = lci;
            _treeView.Invoke(new AddTreeNodeDelegate(this.AddTreeNode), new object[] { parentNode, newNode });

            treeNode = newNode;
            loggerConfigInfo = lci;
        }

        delegate void AddTreeNodeDelegate(TreeNode parentNode, TreeNode childNode);

        private void AddTreeNode(TreeNode parentNode, TreeNode childNode)
        {
            parentNode.Nodes.Add(childNode);
            parentNode.Expand();
        }

        internal void ProcessLogEvent(LogEvent eventInfo)
        {
            ListViewItem item =_listView.Items.Insert(0, eventInfo.ReceivedTime.ToString());
            item.SubItems.Add(eventInfo.Logger);
            item.SubItems.Add(eventInfo.Level);
            item.SubItems.Add(eventInfo.MessageText);

            switch (eventInfo.Level[0])
            {
                case 'T':
                    item.ForeColor = Color.Gray;
                    break;

                case 'D':
                    item.ForeColor = Color.Navy;
                    break;

                case 'I':
                    item.ForeColor = Color.Black;
                    break;

                case 'W':
                    item.ForeColor = Color.Brown;
                    break;

                case 'E':
                    item.ForeColor = Color.Red;
                    break;

                case 'F':
                    item.ForeColor = Color.Orange;
                    break;
            }

            LoggerConfig loggerConfigInfo;
            TreeNode treeNode;

            LoggerToNode(eventInfo.Logger, out treeNode, out loggerConfigInfo);
        }
    }
}
