using k8s;
using k8s.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private TreeNode podSecurityPoliciesTreeNode;

        private BackgroundWorker podSecurityPoliciesLoader;

        private void InitializePodSecurityPolicyLoader()
        {
            podSecurityPoliciesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            podSecurityPoliciesLoader.DoWork += PodSecurityPoliciesLoader_DoWork;
            podSecurityPoliciesLoader.RunWorkerCompleted += PodSecurityPoliciesLoader_RunWorkerCompleted;
            container.Add(podSecurityPoliciesLoader);

            podSecurityPoliciesTreeNode = new TreeNode
            {
                Text = "Pod Security Policies",
                Tag = "psp",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void PodSecurityPoliciesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListPodSecurityPolicy(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new Extensionsv1beta1PodSecurityPolicyList()
                    {
                        Items = new List<Extensionsv1beta1PodSecurityPolicy>(),
                    };
                }
            }
        }

        private void PodSecurityPoliciesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load pod security policies due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load pod security policies due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var podSecurityPoliciesList = e.Result as Extensionsv1beta1PodSecurityPolicyList;
            if (podSecurityPoliciesList == null)
            {
                MessageBox.Show(this,
                    "Cannot load pod security policies due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            podSecurityPoliciesTreeNode.Nodes.Clear();
            var podSecurityPoliciesNodes = new List<TreeNode>();
            for (int i = 0, max = podSecurityPoliciesList.Items.Count; i < max; i++)
            {
                podSecurityPoliciesNodes.Add(new TreeNode(podSecurityPoliciesList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, podSecurityPoliciesList.Items[i]),
                    ImageKey = "psp",
                    SelectedImageKey = "psp",
                });
            }
            podSecurityPoliciesTreeNode.Nodes.AddRange(podSecurityPoliciesNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, Extensionsv1beta1PodSecurityPolicy o, StringBuilder buffer)
        {
            var fetched = await client.ReadPodSecurityPolicyAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Pod Security Policy - {fetched.Metadata.Name}";
        }
    }
}
