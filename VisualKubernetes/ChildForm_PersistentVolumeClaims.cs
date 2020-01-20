using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandPersistentVolumeClaimsResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1PersistentVolumeClaimList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker persistentVolumeClaimsLoader;

        private void InitializePersistentVolumeClaimsLoader()
        {
            persistentVolumeClaimsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            persistentVolumeClaimsLoader.DoWork += PersistentVolumeClaimsLoader_DoWork;
            persistentVolumeClaimsLoader.RunWorkerCompleted += PersistentVolumeClaimsLoader_RunWorkerCompleted;
            container.Add(persistentVolumeClaimsLoader);
        }

        private void PersistentVolumeClaimsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandPersistentVolumeClaimsResponse(
                args.Item1,
                client.ListNamespacedPersistentVolumeClaim(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void PersistentVolumeClaimsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load persistent volume claims due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load persistent volume claims due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandPersistentVolumeClaimsResponse;
            var persistentVolumeClaimsList = response.Item2;
            if (persistentVolumeClaimsList == null)
            {
                MessageBox.Show(this,
                    "Cannot load persistent volume claims due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var persistentVolumeClaimsTreeNode = response.Item3;
            persistentVolumeClaimsTreeNode.Nodes.Clear();
            var persistentVolumeClaimNodes = new List<TreeNode>();
            for (int i = 0, max = persistentVolumeClaimsList.Items.Count; i < max; i++)
            {
                var eachPersistentVolumeClaimTreeNode = new TreeNode(persistentVolumeClaimsList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, persistentVolumeClaimsList.Items[i]),
                    ImageKey = "pvc",
                    SelectedImageKey = "pvc",
                };
                persistentVolumeClaimNodes.Add(eachPersistentVolumeClaimTreeNode);
            }
            persistentVolumeClaimsTreeNode.Nodes.AddRange(persistentVolumeClaimNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1PersistentVolumeClaim o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedPersistentVolumeClaimAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Persistent Volume Claim - {fetched.Metadata.Name}";
        }
    }
}
