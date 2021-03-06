﻿using k8s;
using k8s.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandConfigMapResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1ConfigMapList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    public sealed class ChildForm_ConfigMaps : ISubComponent<V1ConfigMap>
    {
        public ChildForm_ConfigMaps(IWin32Window parentWindow, Func<string> text, Func<KubernetesClientConfiguration> k8sConfig, IContainer container)
        {
            this.parentWindow = parentWindow;
            this.text = text;
            this.k8sConfig = k8sConfig;
            this.container = container;
        }

        private readonly IWin32Window parentWindow;
        private readonly Func<string> text;
        private readonly Func<KubernetesClientConfiguration> k8sConfig;
        private readonly IContainer container;

        private BackgroundWorker configMapsLoader;

        public void Initialize()
        {
            configMapsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            configMapsLoader.DoWork += ConfigMapsLoader_DoWork;
            configMapsLoader.RunWorkerCompleted += ConfigMapsLoader_RunWorkerCompleted;
            container.Add(configMapsLoader);
        }

        private void ConfigMapsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig.Invoke());
            e.Result = new ExpandConfigMapResponse(
                args.Item1,
                client.ListNamespacedConfigMap(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void ConfigMapsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(parentWindow,
                    $"Cannot load config maps due to error - {e.Error.Message}",
                    text.Invoke(), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(parentWindow,
                    "Cannot load config maps due to user's cancellation request.",
                    text.Invoke(), MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandConfigMapResponse;
            var configMapList = response.Item2;
            if (configMapList == null)
            {
                MessageBox.Show(parentWindow,
                    "Cannot load config maps due to error, but reason is unknown.",
                    text.Invoke(), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var configMapsTreeNode = response.Item3;
            configMapsTreeNode.Nodes.Clear();
            var configMapNodes = new List<TreeNode>();
            for (int i = 0, max = configMapList.Items.Count; i < max; i++)
            {
                var eachConfigMapTreeNode = new TreeNode(configMapList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, configMapList.Items[i]),
                    ImageKey = "cm",
                    SelectedImageKey = "cm",
                };
                configMapNodes.Add(eachConfigMapTreeNode);
            }
            configMapsTreeNode.Nodes.AddRange(configMapNodes.ToArray());
        }

        public async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1ConfigMap o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedConfigMapAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(ChildForm.DescribeMetadata(fetched.Metadata));
            return $"Config Map - {fetched.Metadata.Name}";
        }

        public void Run(ExpandRequest expandRequest)
        {
            if (configMapsLoader.IsBusy)
                return;

            configMapsLoader.RunWorkerAsync(expandRequest);
        }
    }
}
