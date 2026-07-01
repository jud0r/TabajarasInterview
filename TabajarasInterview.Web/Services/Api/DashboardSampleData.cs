using TabajarasInterview.Web.DTOs.Dashboard;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// Representative, deterministic sample data for the recruitment dashboard.
    /// Used by the dashboard page as a graceful fallback while the aggregated
    /// <c>GET /api/dashboard/overview</c> endpoint is not yet available, so the UI
    /// stays demonstrable. The page clearly flags when this data is shown.
    /// </summary>
    public static class DashboardSampleData
    {
        public static DashboardData Build()
        {
            var today = DateTime.UtcNow.Date;

            return new DashboardData
            {
                Kpis = new DashboardKpis
                {
                    TotalCandidates = 248,
                    ActivePositions = 12,
                    TotalApplications = 412,
                    ApplicationsInProgress = 86,
                    CompletedApplications = 326,
                    AverageFinalScore = 7.4,
                    AverageInterviewScore = 7.1,
                    PassRate = 38.0
                },
                Funnel =
                [
                    new FunnelStage { Stage = "Applied", Count = 412 },
                    new FunnelStage { Stage = "In Interview", Count = 198 },
                    new FunnelStage { Stage = "Offer / Approved", Count = 86 },
                    new FunnelStage { Stage = "Rejected", Count = 168, IsTerminal = true }
                ],
                StatusDistribution =
                [
                    new StatusSlice { Status = "Applied", Count = 130 },
                    new StatusSlice { Status = "In Interview", Count = 86 },
                    new StatusSlice { Status = "Offer", Count = 52 },
                    new StatusSlice { Status = "Hired", Count = 34 },
                    new StatusSlice { Status = "Rejected", Count = 110 }
                ],
                InterviewTypes =
                [
                    new InterviewTypeStat { Type = "HR", Count = 180, AverageScore = 7.6, SuccessRate = 72 },
                    new InterviewTypeStat { Type = "Technical", Count = 210, AverageScore = 6.4, SuccessRate = 54 },
                    new InterviewTypeStat { Type = "Cultural", Count = 90, AverageScore = 7.9, SuccessRate = 78 },
                    new InterviewTypeStat { Type = "System Design", Count = 64, AverageScore = 6.1, SuccessRate = 48 }
                ],
                AverageInterviewsPerCandidate = 2.3,
                InterviewSuccessRate = 61.0,
                Reviewers =
                [
                    new ReviewerStat { Reviewer = "Diego Alves", InterviewsConducted = 70, AverageScore = 8.4, BiasDelta = 1.3 },
                    new ReviewerStat { Reviewer = "Ana Pereira", InterviewsConducted = 64, AverageScore = 7.9, BiasDelta = 0.8 },
                    new ReviewerStat { Reviewer = "Carla Dias", InterviewsConducted = 48, AverageScore = 7.1, BiasDelta = 0.0 },
                    new ReviewerStat { Reviewer = "Bruno Costa", InterviewsConducted = 52, AverageScore = 6.2, BiasDelta = -0.9 },
                    new ReviewerStat { Reviewer = "Elisa Moreira", InterviewsConducted = 40, AverageScore = 5.6, BiasDelta = -1.5 }
                ],
                TopCandidates =
                [
                    new TopCandidate { Id = 101, Name = "Mariana Lopes", Position = "Backend Engineer", FinalScore = 9.4, Status = "Hired" },
                    new TopCandidate { Id = 102, Name = "Rafael Souza", Position = "Data Scientist", FinalScore = 9.1, Status = "Offer" },
                    new TopCandidate { Id = 103, Name = "Camila Rocha", Position = "Frontend Engineer", FinalScore = 8.9, Status = "Hired" },
                    new TopCandidate { Id = 104, Name = "Lucas Martins", Position = "DevOps Engineer", FinalScore = 8.7, Status = "In Interview" },
                    new TopCandidate { Id = 105, Name = "Beatriz Nunes", Position = "Backend Engineer", FinalScore = 8.6, Status = "Offer" },
                    new TopCandidate { Id = 106, Name = "Thiago Ferreira", Position = "Product Designer", FinalScore = 8.4, Status = "Hired" }
                ],
                RecentCandidates =
                [
                    new RecentCandidate { Id = 201, Name = "Júlia Carvalho", Position = "Frontend Engineer", CreatedAt = today.AddDays(-1) },
                    new RecentCandidate { Id = 202, Name = "Pedro Henrique", Position = "QA Engineer", CreatedAt = today.AddDays(-2) },
                    new RecentCandidate { Id = 203, Name = "Sofia Almeida", Position = "Data Scientist", CreatedAt = today.AddDays(-3) },
                    new RecentCandidate { Id = 204, Name = "Gabriel Lima", Position = "Backend Engineer", CreatedAt = today.AddDays(-4) },
                    new RecentCandidate { Id = 205, Name = "Larissa Pinto", Position = "DevOps Engineer", CreatedAt = today.AddDays(-5) }
                ],
                CandidatesPerPosition =
                [
                    new NamedCount { Label = "Backend Engineer", Count = 42 },
                    new NamedCount { Label = "Frontend Engineer", Count = 38 },
                    new NamedCount { Label = "Data Scientist", Count = 26 },
                    new NamedCount { Label = "QA Engineer", Count = 22 },
                    new NamedCount { Label = "DevOps Engineer", Count = 18 },
                    new NamedCount { Label = "Product Designer", Count = 14 }
                ],
                Positions =
                [
                    new PositionStat { Id = 1, Position = "Backend Engineer", IsOpen = true, Applications = 96, AverageScore = 7.6, TopStack = ".NET" },
                    new PositionStat { Id = 2, Position = "Frontend Engineer", IsOpen = true, Applications = 84, AverageScore = 7.2, TopStack = "React" },
                    new PositionStat { Id = 3, Position = "Data Scientist", IsOpen = true, Applications = 58, AverageScore = 6.8, TopStack = "Python" },
                    new PositionStat { Id = 4, Position = "DevOps Engineer", IsOpen = false, Applications = 44, AverageScore = 7.0, TopStack = "AWS" },
                    new PositionStat { Id = 5, Position = "QA Engineer", IsOpen = true, Applications = 52, AverageScore = 6.5, TopStack = "Selenium" },
                    new PositionStat { Id = 6, Position = "Product Designer", IsOpen = false, Applications = 38, AverageScore = 7.8, TopStack = "Figma" }
                ],
                DemandingStacks =
                [
                    new StackDemand { Stack = "React", OpenPositions = 9, Applications = 120 },
                    new StackDemand { Stack = ".NET", OpenPositions = 7, Applications = 98 },
                    new StackDemand { Stack = "PostgreSQL", OpenPositions = 6, Applications = 85 },
                    new StackDemand { Stack = "AWS", OpenPositions = 5, Applications = 72 },
                    new StackDemand { Stack = "Docker", OpenPositions = 6, Applications = 80 },
                    new StackDemand { Stack = "Rust", OpenPositions = 4, Applications = 60 }
                ],
                Questions =
                [
                    new QuestionStat { Question = "Explain async/await and the task scheduler", TimesUsed = 142, AverageScore = 6.8 },
                    new QuestionStat { Question = "Design a URL shortener", TimesUsed = 118, AverageScore = 5.4 },
                    new QuestionStat { Question = "Normalize a relational schema", TimesUsed = 104, AverageScore = 7.1 },
                    new QuestionStat { Question = "Implement a concurrent rate limiter", TimesUsed = 92, AverageScore = 4.6 },
                    new QuestionStat { Question = "Describe a CI/CD pipeline you built", TimesUsed = 88, AverageScore = 7.9 },
                    new QuestionStat { Question = "Optimize a slow SQL query", TimesUsed = 76, AverageScore = 4.2 }
                ],
                TimeMetrics = new TimeMetrics
                {
                    AverageHiringDays = 23.5,
                    InterviewTurnaroundHours = 41.2,
                    StageDurations =
                    [
                        new StageDuration { Stage = "Screening", AverageDays = 3.2 },
                        new StageDuration { Stage = "Interviews", AverageDays = 9.8 },
                        new StageDuration { Stage = "Review", AverageDays = 4.5 },
                        new StageDuration { Stage = "Offer", AverageDays = 6.0 }
                    ]
                },
                HiringOverTime =
                [
                    new TimePoint { Period = "Jan", Applications = 38, Hires = 4 },
                    new TimePoint { Period = "Feb", Applications = 44, Hires = 5 },
                    new TimePoint { Period = "Mar", Applications = 52, Hires = 6 },
                    new TimePoint { Period = "Apr", Applications = 49, Hires = 5 },
                    new TimePoint { Period = "May", Applications = 61, Hires = 8 },
                    new TimePoint { Period = "Jun", Applications = 58, Hires = 7 },
                    new TimePoint { Period = "Jul", Applications = 66, Hires = 9 },
                    new TimePoint { Period = "Aug", Applications = 44, Hires = 6 }
                ],
                Alerts =
                [
                    new DashboardAlert
                    {
                        Severity = "warning",
                        Title = "Low-score candidates in pipeline",
                        Message = "7 active candidates have an average interview score below 4.0 — review before advancing."
                    },
                    new DashboardAlert
                    {
                        Severity = "warning",
                        Title = "Slow hiring process",
                        Message = "Backend Engineer applications are averaging 31 days to close, above the 25-day target."
                    },
                    new DashboardAlert
                    {
                        Severity = "info",
                        Title = "Reviewer bias detected",
                        Message = "Elisa Moreira scores 1.5 points below the global average; consider calibration."
                    }
                ]
            };
        }
    }
}
