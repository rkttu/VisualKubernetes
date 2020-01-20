using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using ExpandSecretResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1SecretList, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker secretsLoader;

        private void InitializeSecretLoader()
        {
            secretsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            secretsLoader.DoWork += SecretsLoader_DoWork;
            secretsLoader.RunWorkerCompleted += SecretsLoader_RunWorkerCompleted;
            container.Add(secretsLoader);
        }

        private void SecretsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandSecretResponse(
                args.Item1,
                client.ListNamespacedSecret(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void SecretsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load secrets due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load secrets due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandSecretResponse;
            var secretList = response.Item2;
            if (secretList == null)
            {
                MessageBox.Show(this,
                    "Cannot load secrets due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var secretsTreeNode = response.Item3;
            secretsTreeNode.Nodes.Clear();
            var secretNodes = new List<TreeNode>();
            for (int i = 0, max = secretList.Items.Count; i < max; i++)
            {
                var eachSecretTreeNode = new TreeNode(secretList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, secretList.Items[i]),
                    ImageKey = "secrets",
                    SelectedImageKey = "secrets",
                };
                secretNodes.Add(eachSecretTreeNode);
            }
            secretsTreeNode.Nodes.AddRange(secretNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1Secret o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedSecretAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Secret - {fetched.Metadata.Name}";
        }
    }
}
