using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using ExpandServiceAccountResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1ServiceAccountList, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker serviceAccountsLoader;

        private void InitializeServiceAccountLoader()
        {
            serviceAccountsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            serviceAccountsLoader.DoWork += ServiceAccountsLoader_DoWork;
            serviceAccountsLoader.RunWorkerCompleted += ServiceAccountsLoader_RunWorkerCompleted;
            container.Add(serviceAccountsLoader);
        }

        private void ServiceAccountsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandServiceAccountResponse(
                args.Item1,
                client.ListNamespacedServiceAccount(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void ServiceAccountsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load service accounts due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load service accounts due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandServiceAccountResponse;
            var serviceAccountList = response.Item2;
            if (serviceAccountList == null)
            {
                MessageBox.Show(this,
                    "Cannot load service accounts due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var serviceAccountsTreeNode = response.Item3;
            serviceAccountsTreeNode.Nodes.Clear();
            var serviceAccountNodes = new List<TreeNode>();
            for (int i = 0, max = serviceAccountList.Items.Count; i < max; i++)
            {
                var eachServiceAccountTreeNode = new TreeNode(serviceAccountList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, serviceAccountList.Items[i]),
                    ImageKey = "sa",
                    SelectedImageKey = "sa",
                };
                serviceAccountNodes.Add(eachServiceAccountTreeNode);
            }
            serviceAccountsTreeNode.Nodes.AddRange(serviceAccountNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1ServiceAccount o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedServiceAccountAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Service Account - {fetched.Metadata.Name}";
        }
    }
}
