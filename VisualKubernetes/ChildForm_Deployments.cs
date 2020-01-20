using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandDeploymentResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1DeploymentList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker deploymentsLoader;

        private void InitializeDeploymentLoader()
        {
            deploymentsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            deploymentsLoader.DoWork += DeploymentsLoader_DoWork;
            deploymentsLoader.RunWorkerCompleted += DeploymentsLoader_RunWorkerCompleted;
            container.Add(deploymentsLoader);
        }

        private void DeploymentsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandDeploymentResponse(
                args.Item1,
                client.ListNamespacedDeployment(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void DeploymentsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load deployments due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load deployments due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandDeploymentResponse;
            var deploymentList = response.Item2;
            if (deploymentList == null)
            {
                MessageBox.Show(this,
                    "Cannot load deployments due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var deploymentsTreeNode = response.Item3;
            deploymentsTreeNode.Nodes.Clear();
            var deploymentNodes = new List<TreeNode>();
            for (int i = 0, max = deploymentList.Items.Count; i < max; i++)
            {
                var eachDeploymentTreeNode = new TreeNode(deploymentList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, deploymentList.Items[i]),
                    ImageKey = "deploy",
                    SelectedImageKey = "deploy",
                };
                deploymentNodes.Add(eachDeploymentTreeNode);
            }
            deploymentsTreeNode.Nodes.AddRange(deploymentNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1Deployment o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedDeploymentAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Deployment - {fetched.Metadata.Name}";
        }
    }
}
