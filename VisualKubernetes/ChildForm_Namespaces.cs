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
        private TreeNode namespacesTreeNode;

        private BackgroundWorker namespacesLoader;

        private void InitializeNamespaceLoader()
        {
            namespacesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            namespacesLoader.DoWork += NamespacesLoader_DoWork;
            namespacesLoader.RunWorkerCompleted += NamespacesLoader_RunWorkerCompleted;
            container.Add(namespacesLoader);

            namespacesTreeNode = new TreeNode
            {
                Text = "Namespaces",
                Tag = "ns",
                ImageKey = "folder",
                SelectedImageKey = "folderopen",
            };
        }

        private void NamespacesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var k8sConfig = e.Argument as KubernetesClientConfiguration;
            if (k8sConfig == null)
                throw new Exception("Cannot recognize kubernetes client configuration object.");

            using var client = new Kubernetes(k8sConfig);
            try { e.Result = client.ListNamespace(); }
            catch (HttpOperationException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    e.Result = new V1NamespaceList()
                    {
                        Items = new List<V1Namespace>(),
                    };
                }
            }
        }

        private void NamespacesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load namespaces due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load namespaces due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var namespacesList = e.Result as V1NamespaceList;
            if (namespacesList == null)
            {
                MessageBox.Show(this,
                    "Cannot load namespaces due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            namespacesTreeNode.Nodes.Clear();
            var namespaceNodes = new List<TreeNode>();
            for (int i = 0, max = namespacesList.Items.Count; i < max; i++)
            {
                var namespaceMetadata = namespacesList.Items[i];
                var eachNamespaceTreeNode = new TreeNode(namespaceMetadata.Metadata.Name)
                {
                    Tag = new NamespacedMetadata(namespaceMetadata, "ns"),
                    ImageKey = "namespace",
                    SelectedImageKey = "namespace",
                };

                eachNamespaceTreeNode.Nodes.Add(new TreeNode("Pods")
                {
                    Tag = new NamespacedMetadata(namespaceMetadata, "pod"),
                    ImageKey = "folder",
                    SelectedImageKey = "folderopen",
                });
                eachNamespaceTreeNode.Nodes.Add(new TreeNode("Services")
                {
                    Tag = new NamespacedMetadata(namespaceMetadata, "svc"),
                    ImageKey = "folder",
                    SelectedImageKey = "folderopen",
                });
                eachNamespaceTreeNode.Nodes.Add(new TreeNode("Config Maps")
                {
                    Tag = new NamespacedMetadata(namespaceMetadata, "cm"),
                    ImageKey = "folder",
                    SelectedImageKey = "folderopen",
                });
                eachNamespaceTreeNode.Nodes.Add(new TreeNode("Endpoints")
                {
                    Tag = new NamespacedMetadata(namespaceMetadata, "ep"),
                    ImageKey = "folder",
                    SelectedImageKey = "folderopen",
                });
                eachNamespaceTreeNode.Nodes.Add(new TreeNode("Limit Ranges")
                {
                    Tag = new NamespacedMetadata(namespaceMetadata, "limits"),
                    ImageKey = "folder",
                    SelectedImageKey = "folderopen",
                });
                eachNamespaceTreeNode.Nodes.Add(new TreeNode("Persistent Volume Claims")
                {
                    Tag = new NamespacedMetadata(namespaceMetadata, "pvc"),
                    ImageKey = "folder",
                    SelectedImageKey = "folderopen",
                });
                eachNamespaceTreeNode.Nodes.Add(new TreeNode("Pod Templates")
                {
                    Tag = new NamespacedMetadata(namespaceMetadata, "podtemplates"),
                    ImageKey = "folder",
                    SelectedImageKey = "folderopen",
                });
                eachNamespaceTreeNode.Nodes.Add(new TreeNode("Replication Controllers")
                {
                    Tag = new NamespacedMetadata(namespaceMetadata, "rc"),
                    ImageKey = "folder",
                    SelectedImageKey = "folderopen",
                });
                eachNamespaceTreeNode.Nodes.Add(new TreeNode("Secrets")
                {
                    Tag = new NamespacedMetadata(namespaceMetadata, "secrets"),
                    ImageKey = "folder",
                    SelectedImageKey = "folderopen",
                });
                eachNamespaceTreeNode.Nodes.Add(new TreeNode("Service Accounts")
                {
                    Tag = new NamespacedMetadata(namespaceMetadata, "sa"),
                    ImageKey = "folder",
                    SelectedImageKey = "folderopen",
                });
                namespaceNodes.Add(eachNamespaceTreeNode);

                // apps namespace
                {
                    var appTreeNode = new TreeNode("Applications")
                    {
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    };
                    appTreeNode.Nodes.Add(new TreeNode("Controller Revisions")
                    {
                        Tag = new NamespacedMetadata(namespaceMetadata, "controllerrevisions"),
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    });
                    appTreeNode.Nodes.Add(new TreeNode("Daemon Sets")
                    {
                        Tag = new NamespacedMetadata(namespaceMetadata, "ds"),
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    });
                    appTreeNode.Nodes.Add(new TreeNode("Deployments")
                    {
                        Tag = new NamespacedMetadata(namespaceMetadata, "deploy"),
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    });
                    appTreeNode.Nodes.Add(new TreeNode("Replica Sets")
                    {
                        Tag = new NamespacedMetadata(namespaceMetadata, "rs"),
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    });
                    appTreeNode.Nodes.Add(new TreeNode("Stateful Sets")
                    {
                        Tag = new NamespacedMetadata(namespaceMetadata, "sts"),
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    });
                    eachNamespaceTreeNode.Nodes.Add(appTreeNode);
                }

                // autoscaling namespace
                {
                    var autoScalingTreeNode = new TreeNode("Auto Scaling")
                    {
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    };
                    autoScalingTreeNode.Nodes.Add(new TreeNode("Horizontal Pod Auto Scalers")
                    {
                        Tag = new NamespacedMetadata(namespaceMetadata, "hpa"),
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    });
                    eachNamespaceTreeNode.Nodes.Add(autoScalingTreeNode);
                }

                // batch namespace
                {
                    var batchTreeNode = new TreeNode("Batch")
                    {
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    };
                    batchTreeNode.Nodes.Add(new TreeNode("Cron Jobs")
                    {
                        Tag = new NamespacedMetadata(namespaceMetadata, "cj"),
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    });
                    batchTreeNode.Nodes.Add(new TreeNode("Jobs")
                    {
                        Tag = new NamespacedMetadata(namespaceMetadata, "jobs"),
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    });
                    eachNamespaceTreeNode.Nodes.Add(batchTreeNode);
                }

                // extensions namespace
                {
                    var extensionsTreeNode = new TreeNode("Extensions")
                    {
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    };
                    extensionsTreeNode.Nodes.Add(new TreeNode("Ingress")
                    {
                        Tag = new NamespacedMetadata(namespaceMetadata, "ing"),
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    });
                    eachNamespaceTreeNode.Nodes.Add(extensionsTreeNode);
                }

                // policy namespace
                {
                    var policyTreeNode = new TreeNode("Policy")
                    {
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    };
                    policyTreeNode.Nodes.Add(new TreeNode("Pod Disruption Budgets")
                    {
                        Tag = new NamespacedMetadata(namespaceMetadata, "pdb"),
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    });
                    eachNamespaceTreeNode.Nodes.Add(policyTreeNode);
                }

                // misc
                {
                    var miscTreeNode = new TreeNode("Misc")
                    {
                        ImageKey = "folder",
                        SelectedImageKey = "folderopen",
                    };

                    // coordination.k8s.io
                    {
                        var coordinationTreeNode = new TreeNode("Coordination")
                        {
                            ImageKey = "folder",
                            SelectedImageKey = "folderopen",
                        };
                        coordinationTreeNode.Nodes.Add(new TreeNode("Leases")
                        {
                            Tag = new NamespacedMetadata(namespaceMetadata, "lease"),
                            ImageKey = "folder",
                            SelectedImageKey = "folderopen",
                        });
                        miscTreeNode.Nodes.Add(coordinationTreeNode);
                    }

                    // events.k8s.io
                    {
                        var eventsTreeNode = new TreeNode("Events")
                        {
                            ImageKey = "folder",
                            SelectedImageKey = "folderopen",
                        };
                        eventsTreeNode.Nodes.Add(new TreeNode("Events")
                        {
                            Tag = new NamespacedMetadata(namespaceMetadata, "event"),
                            ImageKey = "folder",
                            SelectedImageKey = "folderopen",
                        });
                        miscTreeNode.Nodes.Add(eventsTreeNode);
                    }

                    // networking.k8s.io
                    {
                        var networkingTreeNode = new TreeNode("Networking")
                        {
                            ImageKey = "folder",
                            SelectedImageKey = "folderopen",
                        };
                        networkingTreeNode.Nodes.Add(new TreeNode("Network Policies")
                        {
                            Tag = new NamespacedMetadata(namespaceMetadata, "netpol"),
                            ImageKey = "folder",
                            SelectedImageKey = "folderopen",
                        });
                        miscTreeNode.Nodes.Add(networkingTreeNode);
                    }

                    // rbac.k8s.io
                    {
                        var rbacTreeNode = new TreeNode("Role Based Access Control")
                        {
                            ImageKey = "folder",
                            SelectedImageKey = "folderopen",
                        };
                        rbacTreeNode.Nodes.Add(new TreeNode("Role Bindings")
                        {
                            Tag = new NamespacedMetadata(namespaceMetadata, "rolebinding"),
                            ImageKey = "folder",
                            SelectedImageKey = "folderopen",
                        });
                        rbacTreeNode.Nodes.Add(new TreeNode("Roles")
                        {
                            Tag = new NamespacedMetadata(namespaceMetadata, "role"),
                            ImageKey = "folder",
                            SelectedImageKey = "folderopen",
                        });
                        miscTreeNode.Nodes.Add(rbacTreeNode);
                    }

                    eachNamespaceTreeNode.Nodes.Add(miscTreeNode);
                }
            }
            namespacesTreeNode.Nodes.AddRange(namespaceNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespaceAsync(o.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Namespace - {fetched.Metadata.Name}";
        }
    }
}
