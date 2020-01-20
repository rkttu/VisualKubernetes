using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using ExpandServiceResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1ServiceList, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker servicesLoader;

        private void InitializeServiceLoader()
        {
            servicesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            servicesLoader.DoWork += ServicesLoader_DoWork;
            servicesLoader.RunWorkerCompleted += ServicesLoader_RunWorkerCompleted;
            container.Add(servicesLoader);
        }

        private void ServicesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandServiceResponse(
                args.Item1,
                client.ListNamespacedService(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void ServicesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load services due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load services due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandServiceResponse;
            var svcList = response.Item2;
            if (svcList == null)
            {
                MessageBox.Show(this,
                    "Cannot load services due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var svcsTreeNode = response.Item3;
            svcsTreeNode.Nodes.Clear();
            var svcNodes = new List<TreeNode>();
            for (int i = 0, max = svcList.Items.Count; i < max; i++)
            {
                var eachSvcTreeNode = new TreeNode(svcList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, svcList.Items[i]),
                    ImageKey = "svc",
                    SelectedImageKey = "svc",
                };
                svcNodes.Add(eachSvcTreeNode);
            }
            svcsTreeNode.Nodes.AddRange(svcNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1Service o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedServiceAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Service - {fetched.Metadata.Name}";
        }
    }
}
