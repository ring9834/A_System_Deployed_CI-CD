# CI additions for Step 4 (append to .github/workflows/ci.yml from Step 0)

Add these as new jobs. Requires GitHub secrets/variables:
- AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID (OIDC federated credential —
  set up in Azure AD + GitHub repo settings, cannot be automated from here)
- No long-lived Azure secret — authenticate via OIDC (`azure/login@v2` with
  `permissions: id-token: write`)

```yaml
  build-and-push:
    needs: [api, web]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    permissions:
      contents: read
      id-token: write
    steps:
      - uses: actions/checkout@v4
      - uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - name: ACR login
        run: az acr login --name <your-acr-name>
      - name: Build and push API image
        run: |
          docker build -t <acr>.azurecr.io/todo-api:${{ github.sha }} api/
          docker push <acr>.azurecr.io/todo-api:${{ github.sha }}
      - name: Build and push web image
        run: |
          docker build -t <acr>.azurecr.io/todo-web:${{ github.sha }} web/
          docker push <acr>.azurecr.io/todo-web:${{ github.sha }}

  terraform:
    runs-on: ubuntu-latest
    defaults: { run: { working-directory: infra/terraform } }
    steps:
      - uses: actions/checkout@v4
      - uses: hashicorp/setup-terraform@v3
      - run: terraform fmt -check
      - run: terraform init
      - run: terraform validate
      - run: terraform plan   # apply stays a manual/approved step for now, not automatic

  helm-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: azure/setup-helm@v4
      - run: helm lint deploy/helm/todo-planner
      - run: helm template deploy/helm/todo-planner | kubectl apply --dry-run=client -f -

  deploy-dev:
    needs: [build-and-push, terraform, helm-check]
    runs-on: ubuntu-latest
    environment: dev   # add an approval gate here once you're past the skeleton stage
    permissions: { id-token: write }
    steps:
      - uses: actions/checkout@v4
      - uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - uses: azure/aks-set-context@v4
        with: { resource-group: <rg-name>, cluster-name: <aks-name> }
      - uses: azure/setup-helm@v4
      - run: |
          helm upgrade --install todo deploy/helm/todo-planner \
            --set api.image.tag=${{ github.sha }} \
            --set web.image.tag=${{ github.sha }} \
            --atomic --timeout 5m
      - name: Smoke test
        run: curl -sf https://<dev-hostname>/api/health
```

Fill in `<acr>`, `<rg-name>`, `<aks-name>`, `<dev-hostname>` once Terraform's first apply
gives you real values (its `outputs.tf` prints `acr_login_server`, `aks_cluster_name`,
`resource_group_name`).
