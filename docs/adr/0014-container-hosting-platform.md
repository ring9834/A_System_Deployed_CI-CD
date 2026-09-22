# 0014: Container hosting platform — AKS, not ACA/ACI/App Service for Containers

## Context
Unlike most decisions in this document, this one is constrained by an explicit brief
requirement, not an open architectural question: "hosting infrastructure should... be
hosted using Kubernetes, and deployed to using Helm." This is stated without an "or" —
contrast with the brief's AWS-or-Azure framing, which genuinely is a free choice.

## Options
1. **AKS (Azure Kubernetes Service)** — managed Kubernetes control plane; full K8s API,
   real Deployments/Services/Ingress/HPA/PDB objects, native Helm support.
2. **Azure Container Apps (ACA)** — serverless container platform (KEDA/Envoy under the
   hood), its own resource model, not the Kubernetes API; no Helm.
3. **Azure Container Instances (ACI)** — run a single container/container group on demand;
   no orchestration, no ingress routing, no rolling deploys.
4. **App Service for Containers** — PaaS web app hosting a container image; basic
   autoscale/slots, not Kubernetes concepts.

## Decision
Option 1: AKS.

## Rationale
- **On pure engineering fit for a todo CRUD API, AKS is arguably the wrong-sized tool.**
  ACA (2) and App Service (4) would both be strong, lower-overhead choices on their own
  merits — this is stated plainly, not glossed over.
- **All three alternatives are disqualified regardless of fit**, because none run the
  Kubernetes API and none support Helm — both explicitly named in the brief. This isn't a
  trade-off between options that all satisfy the requirement; only AKS does.
- AKS's operational overhead (cluster lifecycle, node management, networking, upgrades) is
  real and higher than the app's actual complexity would organically justify. This is an
  accepted cost of compliance with the brief, not a claim that Kubernetes is the "correct"
  architecture for an app this size.

## Consequences
- Every Helm/Kubernetes artefact already designed (Deployment, probes, PDB, HPA, ingress
  routing per ADR-0002) is now confirmed necessary, not optional scope.
- To avoid the size mismatch becoming worse than it has to be: use a minimal node pool
  (one or two small nodes, e.g. `Standard_B2s`), the free AKS control-plane tier, no service
  mesh, no unnecessary cluster add-ons — right-size everything *within* the mandated
  platform, the same principle applied in ADR-0001/0009/0011/0013 to other decisions.
- Cost/time control for the 30-day Azure trial: start with the smallest viable node pool,
  scale down or delete outside active work sessions, and `terraform destroy` when the
  assessment is complete.
- If asked "isn't this overkill," the honest answer is yes for the app's actual size — AKS is
  here because the brief requires it, not because it was the natural architectural choice.
