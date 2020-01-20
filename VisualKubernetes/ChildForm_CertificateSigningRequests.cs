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
        private TreeNode certificateSigningRequestsTreeNode;

        private BackgroundWorker certificateSigningRequestsLoader;

        private void InitializeCertificateSigningRequestLoader()
        {
            certificateSigningRequestsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            certificateSigningRequestsLoader.DoWork += CertificateSigningRequestsLoader_DoWork;
            certificateSigningRequestsLoader.RunWorkerCompleted += CertificateSigningRequestsLoader_RunWorkerCompleted;
            container.Add(certificateSigningRequestsLoader);

            certificateSigningRequestsTreeNode = new TreeNode
            {
                Text = "Certificate Signing Requests",
                Tag = "csr",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void CertificateSigningRequestsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListCertificateSigningRequest(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1beta1CertificateSigningRequestList()
                    {
                        Items = new List<V1beta1CertificateSigningRequest>(),
                    };
                }
            }
        }

        private void CertificateSigningRequestsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load certificate signing requests due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load certificate signing requests due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var certificateSigningRequestsList = e.Result as V1beta1CertificateSigningRequestList;
            if (certificateSigningRequestsList == null)
            {
                MessageBox.Show(this,
                    "Cannot load certificate signing requests due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            certificateSigningRequestsTreeNode.Nodes.Clear();
            var certificateSigningRequestsNodes = new List<TreeNode>();
            for (int i = 0, max = certificateSigningRequestsList.Items.Count; i < max; i++)
            {
                certificateSigningRequestsNodes.Add(new TreeNode(certificateSigningRequestsList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(null, certificateSigningRequestsList.Items[i]),
                    ImageKey = "csr",
                    SelectedImageKey = "csr",
                });
            }
            certificateSigningRequestsTreeNode.Nodes.AddRange(certificateSigningRequestsNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1beta1CertificateSigningRequest o, StringBuilder buffer)
        {
            var fetched = await client.ReadCertificateSigningRequestAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Certificate Signing Request - {fetched.Metadata.Name}";
        }
    }
}
