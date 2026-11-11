using Hive.Core.Entities;

namespace Hive.Application.Services;

/// <summary>
/// The built-in skill roadmaps, in the spirit of roadmap.sh: a role broken into sections of
/// skills, from the fundamentals to the advanced. Adding one to Hive copies it into an ordinary
/// <see cref="SkillRoadmap"/>, which the manager can then edit freely; the library itself never
/// changes what is already in Hive.
/// </summary>
public static class RoadmapLibrary
{
    public sealed record LibraryRoadmap(string Key, string Name, string Description, IReadOnlyList<LibrarySection> Sections)
    {
        public IEnumerable<LibrarySkill> Skills => Sections.SelectMany(s => s.Skills);
        public int HardSkillCount => Sections.Where(s => s.Category == SkillCategory.Hard).Sum(s => s.Skills.Count);
        public int SoftSkillCount => Sections.Where(s => s.Category == SkillCategory.Soft).Sum(s => s.Skills.Count);
    }

    /// <summary>
    /// A section of skills, all hard or all soft, introduced at <paramref name="FromStage"/>.
    /// </summary>
    public sealed record LibrarySection(string Name, SkillCategory Category, IReadOnlyList<LibrarySkill> Skills,
        CareerStage FromStage = CareerStage.Junior);

    public sealed record LibrarySkill(string Title, string Description, bool IsOptional = false);

    public static IReadOnlyList<LibraryRoadmap> All { get; } =
    [
        Backend(),
        Frontend(),
        AspNetCore(),
        DevOps(),
        Qa(),
        SoftwareArchitect(),
        EngineeringManager(),
    ];

    public static LibraryRoadmap? Find(string key) =>
        All.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Builds the roadmap and its skills, ready to save.
    /// </summary>
    public static (SkillRoadmap Roadmap, IReadOnlyList<RoadmapSkill> Skills) Instantiate(LibraryRoadmap source, string? name = null)
    {
        var roadmap = new SkillRoadmap(name ?? source.Name, source.Description, source.Key);
        var sortOrder = 0;
        var skills = source.Sections
            .SelectMany(section => section.Skills.Select(skill => new RoadmapSkill(
                roadmap.Id, section.Name, skill.Title, section.Category, sortOrder++, skill.Description, skill.IsOptional,
                section.FromStage)))
            .ToList();
        return (roadmap, skills);
    }

    private static LibrarySection Hard(string name, params LibrarySkill[] skills) => new(name, SkillCategory.Hard, skills);
    private static LibrarySection Soft(string name, params LibrarySkill[] skills) => new(name, SkillCategory.Soft, skills);

    /// <summary>
    /// A section introduced at a later stage: a plan aiming below it leaves it out.
    /// </summary>
    private static LibrarySection From(CareerStage stage, LibrarySection section) => section with { FromStage = stage };
    private static LibrarySkill S(string title, string description) => new(title, description);
    private static LibrarySkill Opt(string title, string description) => new(title, description, IsOptional: true);

    // Soft skills every engineer grows, whatever the stack.
    private static LibrarySection EngineerCommunication() => Soft("Communication",
        S("Written communication", "Clear PR descriptions, design notes and status updates that need no follow-up."),
        S("Explaining technical ideas", "Explains a design or a trade-off to engineers and non-engineers alike."),
        S("Asking for help early", "Raises blockers and unknowns before they cost days."),
        S("Giving and receiving feedback", "Gives specific, kind feedback and acts on feedback received."));

    private static LibrarySection EngineerCollaboration() => Soft("Collaboration",
        S("Code review", "Reviews for correctness and clarity, promptly and constructively."),
        S("Pairing and knowledge sharing", "Pairs, writes things down, and makes knowledge less siloed."),
        S("Working with product", "Understands the why, questions requirements, and proposes simpler options."));

    private static LibrarySection EngineerOwnership() => Soft("Ownership",
        S("Estimation and planning", "Breaks work down and gives estimates that hold up."),
        S("Delivering end to end", "Takes work from idea to production and follows up after release."),
        S("Prioritisation", "Works on what matters most, and says no to what does not."),
        S("Learning mindset", "Picks up new tools and domains, and reflects on mistakes."));

    // What changes at Senior: leading work and growing others, not only doing it.
    private static LibrarySection SeniorLeadership() => From(CareerStage.Senior, Soft("Senior: technical leadership",
        S("Mentoring", "Helps less experienced engineers grow, through pairing, reviews and feedback."),
        S("Technical decision-making", "Makes and documents sound decisions, and weighs trade-offs openly."),
        S("Leading a project", "Takes a multi-person piece of work from plan to delivery."),
        S("Raising the bar", "Sets and models the team's standards for quality, testing and reviews.")));

    // What changes at Staff: scope beyond the team, and influence rather than output.
    private static LibrarySection StaffInfluence() => From(CareerStage.Staff, Soft("Staff: scope and influence",
        S("Cross-team influence", "Aligns several teams on a technical direction without authority over them."),
        S("Technical strategy", "Sees problems a year out and shapes the roadmap to meet them."),
        S("Writing technical vision", "Writes the proposals and visions that others build on."),
        S("Sponsoring others", "Creates opportunities for others and advocates for them."),
        S("Organisational impact", "Improves how engineering works, beyond a single team's code.")));

    private static LibraryRoadmap Backend() => new(
        "backend",
        "Backend Engineer",
        "From how the internet works to scalable, secure and observable services.",
        [
            Hard("Internet fundamentals",
                S("How the internet works", "Packets, routing, and what happens between a request and a response."),
                S("HTTP", "Methods, status codes, headers, caching and HTTP/2."),
                S("DNS and domains", "How names resolve, records, and TTLs."),
                S("Browsers and hosting", "What a browser does with a response, and where code runs.")),
            Hard("Language and tooling",
                S("A backend language in depth", "Idioms, the standard library, memory and concurrency model of the team's main language."),
                S("Git and version control", "Branching, rebasing, resolving conflicts, and clean history."),
                S("Package management", "Dependencies, versions and lock files."),
                Opt("A second backend language", "Enough of another language to read and contribute to it.")),
            Hard("Databases",
                S("Relational databases", "Modelling, normalisation, joins and constraints."),
                S("SQL", "Queries, aggregates, window functions and query plans."),
                S("Indexes and performance", "Choosing indexes and reading an execution plan."),
                S("Transactions and ACID", "Isolation levels, locking and consistency."),
                S("ORMs", "Using an ORM well, and knowing when to drop to SQL."),
                S("Migrations", "Evolving a schema safely with no downtime."),
                Opt("NoSQL databases", "Document, key-value and wide-column stores and when to use them.")),
            Hard("APIs",
                S("REST", "Resource design, versioning, pagination and errors."),
                S("Authentication", "Sessions, tokens, OAuth 2 and OpenID Connect."),
                S("API documentation", "OpenAPI and docs that others can build on."),
                Opt("GraphQL", "Schemas, resolvers and the N+1 problem."),
                Opt("gRPC", "Protocol buffers and streaming RPC.")),
            Hard("Testing",
                S("Unit testing", "Fast, isolated tests of logic."),
                S("Integration testing", "Testing against real databases and services."),
                S("Test design", "What to test, test doubles, and keeping suites maintainable."),
                Opt("Contract testing", "Keeping services' expectations of each other in check.")),
            Hard("Security",
                S("OWASP Top 10", "The common web vulnerabilities and how to prevent them."),
                S("Hashing and encryption", "Password hashing, TLS, and encryption at rest."),
                S("Secrets management", "Keeping credentials out of code and logs."),
                S("Input validation", "Validating and encoding at the boundary.")),
            From(CareerStage.Mid, Hard("Architecture and scale",
                S("Caching", "Client, CDN and server-side caching, and invalidation."),
                S("Message queues", "Asynchronous work, at-least-once delivery and idempotency."),
                S("Design patterns", "The common patterns and when they help."),
                S("Clean architecture and DDD", "Layers, boundaries and a model in the domain's language."),
                S("Scaling", "Horizontal scaling, statelessness, load balancing and replication."),
                Opt("Microservices", "Service boundaries and their operational cost."),
                Opt("Event-driven architecture", "Events, event sourcing and CQRS."))),
            Hard("Running in production",
                S("Containers", "Building and running Docker images."),
                S("CI/CD", "Pipelines that build, test and deploy automatically."),
                S("Observability", "Logs, metrics, traces and useful alerts."),
                S("Incident response", "Debugging production, rollbacks and blameless post-mortems.")),
            EngineerCommunication(),
            EngineerCollaboration(),
            EngineerOwnership(),
            SeniorLeadership(),
            StaffInfluence(),
        ]);

    private static LibraryRoadmap Frontend() => new(
        "frontend",
        "Frontend Engineer",
        "From HTML and CSS to fast, accessible, well-tested web applications.",
        [
            Hard("Web fundamentals",
                S("How the web works", "HTTP, DNS, and what a browser does with a page."),
                S("HTML", "Semantic markup, forms and document structure."),
                S("Accessibility", "WCAG, keyboard navigation, ARIA and screen readers."),
                S("Browser developer tools", "Debugging layout, network and performance.")),
            Hard("CSS",
                S("CSS fundamentals", "The box model, specificity, and the cascade."),
                S("Layout", "Flexbox and grid."),
                S("Responsive design", "Media queries and fluid layouts across devices."),
                S("A CSS approach", "Utility CSS, CSS modules or CSS-in-JS, used consistently."),
                Opt("Animations", "Transitions and animations that respect reduced motion.")),
            Hard("JavaScript and TypeScript",
                S("JavaScript fundamentals", "Scope, closures, prototypes and the event loop."),
                S("Asynchronous JavaScript", "Promises, async/await and error handling."),
                S("The DOM and browser APIs", "Events, fetch, storage and the DOM."),
                S("TypeScript", "Types, generics and narrowing."),
                S("Modules and bundling", "ES modules, and bundlers such as Vite.")),
            Hard("Frameworks",
                S("A component framework", "React, Vue or Angular in depth."),
                S("State management", "Local, shared and server state, and when each fits."),
                S("Routing", "Client-side routing and code splitting."),
                S("Forms", "Validation, error states and accessible forms."),
                Opt("Server-side rendering", "SSR, static generation and hydration.")),
            Hard("Quality",
                S("Unit and component testing", "Testing components the way users use them."),
                S("End-to-end testing", "Testing critical journeys in a real browser."),
                S("Linting and formatting", "Consistent code enforced by tools."),
                Opt("Visual regression testing", "Catching unintended visual changes.")),
            From(CareerStage.Mid, Hard("Performance and security",
                S("Web performance", "Core Web Vitals, lazy loading and caching."),
                S("Rendering performance", "Avoiding needless re-renders and layout thrashing."),
                S("Web security", "XSS, CSRF, CORS and content security policy."))),
            Hard("Delivery",
                S("Git and version control", "Branching, rebasing and clean history."),
                S("CI/CD", "Pipelines that build, test and deploy the app."),
                S("API integration", "Working with REST or GraphQL backends and their errors."),
                Opt("Design systems", "Reusable components and design tokens.")),
            EngineerCommunication(),
            EngineerCollaboration(),
            EngineerOwnership(),
            SeniorLeadership(),
            StaffInfluence(),
        ]);

    private static LibraryRoadmap AspNetCore() => new(
        "aspnet-core",
        "ASP.NET Core Developer",
        "C# and .NET from the language up to production ASP.NET Core services.",
        [
            Hard("C# and .NET",
                S("C# fundamentals", "Types, classes, records and generics."),
                S("LINQ", "Querying collections and understanding deferred execution."),
                S("Async and await", "Tasks, cancellation and avoiding deadlocks."),
                S("The .NET runtime", "The CLR, garbage collection and memory."),
                S(".NET CLI and SDK", "Building, testing and publishing from the command line."),
                Opt("Modern C# features", "Pattern matching, spans and nullable reference types.")),
            Hard("ASP.NET Core",
                S("Middleware pipeline", "How a request flows and writing middleware."),
                S("Dependency injection", "Lifetimes, registration and composition."),
                S("Configuration and options", "Settings, environments and the options pattern."),
                S("Controllers and minimal APIs", "Routing, model binding and results."),
                S("Authentication and authorisation", "Schemes, policies and claims."),
                S("Logging", "Structured logging with ILogger."),
                Opt("Background services", "Hosted services and queued work.")),
            Hard("Data access",
                S("SQL and relational databases", "Modelling, queries and indexes."),
                S("Entity Framework Core", "The DbContext, tracking, relationships and queries."),
                S("EF Core migrations", "Evolving the schema safely."),
                Opt("Dapper", "Lightweight SQL mapping for hot paths.")),
            From(CareerStage.Mid, Hard("Architecture",
                S("Clean architecture", "Layers and the dependency rule."),
                S("Domain-driven design", "Entities, value objects and invariants."),
                S("Design patterns", "Repository, mediator, strategy and friends."),
                Opt("Messaging", "Queues and message buses in .NET."))),
            Hard("Testing",
                S("xUnit", "Facts, theories and fixtures."),
                S("Mocking", "Test doubles with Moq or NSubstitute."),
                S("Integration tests", "WebApplicationFactory and real dependencies."),
                S("Readable assertions", "FluentAssertions and clear failures.")),
            Hard("Production",
                S("Performance", "Profiling, allocations and caching."),
                S("Resilience", "Retries, timeouts and circuit breakers."),
                S("HttpClient done right", "IHttpClientFactory and typed clients."),
                S("Containers and deployment", "Dockerising and publishing .NET apps."),
                Opt("OpenTelemetry", "Traces and metrics from .NET services.")),
            EngineerCommunication(),
            EngineerCollaboration(),
            EngineerOwnership(),
            SeniorLeadership(),
            StaffInfluence(),
        ]);

    private static LibraryRoadmap DevOps() => new(
        "devops",
        "DevOps Engineer",
        "Operating systems to cloud, infrastructure as code and reliable delivery.",
        [
            Hard("Foundations",
                S("A scripting language", "Automating with Bash, Python or PowerShell."),
                S("Linux", "Processes, permissions, services and the shell."),
                S("Networking", "TCP/IP, DNS, load balancers, firewalls and TLS."),
                S("Git", "Branching strategies and repository hygiene.")),
            Hard("Containers and orchestration",
                S("Docker", "Images, layers, registries and compose."),
                S("Kubernetes", "Pods, deployments, services and ingress."),
                S("Helm or Kustomize", "Packaging and configuring deployments."),
                Opt("Service mesh", "Traffic management and mTLS between services.")),
            Hard("Cloud",
                S("A cloud provider", "Core compute, storage and networking on AWS, Azure or GCP."),
                S("Identity and access", "IAM, least privilege and roles."),
                S("Cost awareness", "Understanding and reducing the bill."),
                Opt("Serverless", "Functions and managed services.")),
            Hard("Infrastructure as code",
                S("Terraform or equivalent", "Declaring infrastructure and managing state."),
                S("Configuration management", "Ansible, or immutable images instead."),
                S("GitOps", "Deploying from the repository as the source of truth.")),
            Hard("Delivery",
                S("CI pipelines", "Build, test and scan on every change."),
                S("Deployment strategies", "Blue-green, canary and rollbacks."),
                S("Artifact management", "Versioned builds and registries."),
                S("Secrets management", "Vaults, rotation and keeping secrets out of logs.")),
            From(CareerStage.Mid, Hard("Reliability",
                S("Monitoring and alerting", "Metrics, dashboards and alerts that matter."),
                S("Logging", "Centralised, structured logs."),
                S("SLOs and error budgets", "Measuring reliability the user feels."),
                S("Incident management", "On-call, runbooks and post-mortems."),
                Opt("Chaos engineering", "Testing failure on purpose."))),
            EngineerCommunication(),
            EngineerCollaboration(),
            EngineerOwnership(),
            SeniorLeadership(),
            StaffInfluence(),
        ]);

    private static LibraryRoadmap Qa() => new(
        "qa",
        "QA Engineer",
        "Testing fundamentals through automation, performance and quality advocacy.",
        [
            Hard("Testing fundamentals",
                S("Testing principles", "What testing can and cannot show, and the test pyramid."),
                S("Test design techniques", "Equivalence classes, boundaries and decision tables."),
                S("Exploratory testing", "Structured sessions that find what scripts miss."),
                S("Bug reporting", "Reproducible, prioritised and clear defect reports.")),
            Hard("Manual testing",
                S("Test cases and plans", "Writing and maintaining test cases."),
                S("Regression testing", "Choosing what to re-test for a change."),
                S("Acceptance criteria", "Turning requirements into testable criteria.")),
            Hard("Automation",
                S("A programming language", "Enough to write clean, maintainable test code."),
                S("UI automation", "Playwright, Cypress or Selenium."),
                S("API testing", "Testing REST APIs directly."),
                S("Test frameworks", "Fixtures, data and reporting."),
                S("Flaky test management", "Finding and fixing unreliable tests.")),
            From(CareerStage.Mid, Hard("Beyond functional",
                S("Performance testing", "Load and stress tests with k6, JMeter or similar."),
                S("Security testing basics", "OWASP risks from a tester's view."),
                S("Accessibility testing", "Checking against WCAG with tools and by hand."),
                Opt("Mobile testing", "Devices, emulators and mobile specifics."))),
            Hard("Delivery",
                S("CI integration", "Running tests in pipelines and gating releases."),
                S("Test environments and data", "Reliable environments and test data."),
                S("Git", "Working in the same repository as the code.")),
            Soft("Quality advocacy",
                S("Shifting left", "Getting involved in refinement and design, not only at the end."),
                S("Risk-based thinking", "Testing where failure would hurt most."),
                S("Communicating quality", "Reporting quality clearly to the team and stakeholders.")),
            EngineerCommunication(),
            EngineerCollaboration(),
            SeniorLeadership(),
            StaffInfluence(),
        ]);

    private static LibraryRoadmap SoftwareArchitect() => new(
        "software-architect",
        "Software Architect",
        "Designing systems, making trade-offs visible and leading technical direction.",
        [
            Hard("Design foundations",
                S("Design principles", "SOLID, coupling and cohesion."),
                S("Design patterns", "Patterns at code and system level."),
                S("Domain-driven design", "Bounded contexts and context maps."),
                S("Architecture styles", "Layered, hexagonal, microservices, event-driven, and their costs.")),
            Hard("Systems",
                S("Distributed systems", "Consistency, partitions, CAP and idempotency."),
                S("Data architecture", "Choosing stores, modelling and data ownership."),
                S("Integration", "APIs, messaging and integration patterns."),
                S("Scalability and performance", "Capacity, caching and bottlenecks."),
                S("Security architecture", "Threat modelling, zero trust and defence in depth.")),
            Hard("Operations",
                S("Cloud architecture", "Designing for a cloud provider's building blocks."),
                S("Reliability", "Redundancy, failure modes and recovery."),
                S("Observability design", "Making a system explain itself in production.")),
            Hard("Making decisions",
                S("Architecture decision records", "Recording decisions and their context."),
                S("Quality attributes", "Turning -ilities into measurable scenarios."),
                S("Technical debt management", "Making debt visible and paying it down deliberately."),
                S("Diagramming", "C4 and diagrams that communicate.")),
            Soft("Leadership",
                S("Influencing without authority", "Bringing teams along rather than dictating."),
                S("Stakeholder communication", "Explaining trade-offs to business and engineering."),
                S("Facilitation", "Running design reviews and workshops."),
                S("Mentoring engineers", "Growing architecture thinking in the team.")),
            Soft("Strategy",
                S("Business acumen", "Linking technical choices to business outcomes."),
                S("Technology evaluation", "Assessing new technology soberly."),
                S("Roadmapping", "Planning the technical direction over quarters.")),
        ]);

    private static LibraryRoadmap EngineeringManager() => new(
        "engineering-manager",
        "Engineering Manager",
        "From senior engineer or tech lead to leading people, delivery and strategy.",
        [
            Hard("Technical leadership",
                S("System design", "Enough depth to guide and challenge designs."),
                S("Technical debt strategy", "Balancing debt against features."),
                S("Engineering practices", "Code review, testing and CI standards for a team."),
                S("Architecture decisions", "Enabling good decisions rather than making them all.")),
            Hard("Delivery",
                S("Agile ways of working", "Scrum, Kanban and adapting them to the team."),
                S("Planning and estimation", "Roadmaps, capacity and commitments that hold."),
                S("Delivery metrics", "Velocity, cycle time and DORA metrics, used wisely."),
                S("Risk management", "Spotting and mitigating delivery risks early."),
                S("Incident management", "Leading through incidents and learning from them.")),
            Soft("People management",
                S("1:1s", "Regular, useful 1:1s that the report owns."),
                S("Feedback", "Timely, specific feedback, positive and corrective."),
                S("Career development", "Growth plans and stretch opportunities."),
                S("Performance management", "Setting expectations and handling underperformance."),
                S("Delegation", "Giving away work that grows others."),
                S("Coaching", "Helping people find their own answers.")),
            Soft("Team building",
                S("Hiring", "Structured interviews and fair hiring decisions."),
                S("Onboarding", "Getting new people productive and included."),
                S("Psychological safety", "A team where people speak up."),
                S("Team health", "Spotting burnout, conflict and morale issues."),
                S("Conflict resolution", "Addressing conflict directly and fairly.")),
            Soft("Communication",
                S("Stakeholder management", "Managing expectations up and across."),
                S("Written communication", "Clear updates, proposals and decisions."),
                S("Presenting", "Presenting plans and results with confidence."),
                S("Managing up", "Keeping your manager informed and aligned.")),
            Soft("Strategy and self",
                S("Product thinking", "Understanding users and business goals."),
                S("Organisational awareness", "How the company works and decides."),
                S("Prioritisation", "Choosing what the team does, and does not do."),
                S("Self-management", "Managing your own time, energy and growth."),
                Opt("Budgeting", "Headcount and cost planning.")),
        ]);
}
