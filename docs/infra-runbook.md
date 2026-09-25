# Infra runbook: manual steps not yet captured in Terraform/Helm

These steps are currently run by hand and are **not** reproducible purely from
`terraform apply` + `helm install todo`. Recorded here so they aren't tribal knowledge.
Candidate for automating later (see "Gap" note at the end) — not done now, given the time box.

## One-time cluster setup (after `terraform apply` creates the AKS cluster)

```bash
# point kubectl at the cluster (writes/merges into ~/.kube/config)
az aks get-credentials --resource-group rush-todo-dev-rg --name rush-todo-dev-aks

# install the ingress controller
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update
helm install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace \
  --set controller.replicaCount=1 \
  --set controller.resources.requests.cpu=100m \
  --set controller.resources.requests.memory=90Mi

# get the ingress controller's public IP, and derive a free dev hostname
kubectl get service ingress-nginx-controller -n ingress-nginx \
  -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
# -> update deploy/helm/todo-planner/values.yaml: ingress.host: <that-ip>.nip.io
```

## Cost control between work sessions

The ingress controller's public IP bills continuously while it exists — `az aks stop` does
**not** stop this charge, since the Load Balancer/Public IP are separate resources from the
node compute `aks stop` deallocates. For a short pause, remove just the IP-holding resource:

```bash
helm uninstall ingress-nginx -n ingress-nginx
```

For a full pause (overnight/multi-day), also stop the cluster:
```bash
az aks stop --name rush-todo-dev-aks --resource-group rush-todo-dev-rg
```

**Trade-off:** reinstalling `ingress-nginx` later gets a *new* dynamic IP (unless made
static, which trades this for a continuous charge instead — not done here, deliberately, per
the reasoning above). Re-run the "get the IP" step and update `values.yaml`'s `ingress.host`
each time.

To resume:
```bash
az aks start --name rush-todo-dev-aks --resource-group rush-todo-dev-rg
# then re-run the ingress-nginx install + IP lookup above
```

## Troubleshooting encountered while setting this up

| Symptom | Cause | Fix |
|---|---|---|
| `kubectl` error: `current-context is not set`, connects to `localhost:8080` | No kubeconfig context — `az aks get-credentials` was never run in this shell/machine | Run `az aks get-credentials --resource-group rush-todo-dev-rg --name rush-todo-dev-aks` |
| `helm : term not recognized` | Helm not installed, or PATH not refreshed | `winget install Helm.Helm`, then **close and reopen the terminal** — PATH changes don't apply to already-open shells |
| PowerShell `curl -i <url>` prompts `Supply values for: Uri` | PowerShell aliases `curl` to `Invoke-WebRequest`, which doesn't understand `-i` the same way real curl does | Use `Invoke-WebRequest -Uri <url>`, or call the real binary explicitly: `curl.exe -i <url>` |
| `Invoke-WebRequest` / `curl` to the ingress IP: connection refused/unable to connect | Ingress controller pod not yet `Running`, or LB not fully wired up yet | `kubectl get pods -n ingress-nginx` (wait for `Running`/`1/1`); `kubectl describe service ingress-nginx-controller -n ingress-nginx` (check `Endpoints:` isn't empty); `Test-NetConnection -ComputerName <ip> -Port 80` for a cleaner TCP-level check than an HTTP request |

## Gap this exposes

Ingress-nginx's installation isn't managed as code — a genuine production-readiness gap,
tracked here rather than hidden. A real next step (not done now, given the time box) would be
managing it via Terraform's `helm_release` resource, or at minimum a script
(`scripts/setup-ingress.sh`) wrapping the commands above so "set up a fresh cluster" is one
command, not a remembered sequence.
