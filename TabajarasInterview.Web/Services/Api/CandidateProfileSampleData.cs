using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.DTOs.Candidate;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// Representative, deterministic sample data for the candidate profile page.
    /// Used as a graceful fallback while the
    /// <c>GET /api/candidates/{id}/positions</c> and
    /// <c>GET /api/candidates/{id}/timeline</c> endpoints are not yet available, so
    /// the UI stays demonstrable. The page clearly flags when this data is shown.
    /// </summary>
    public static class CandidateProfileSampleData
    {
        /// <summary>
        /// Builds a small, realistic set of applications (with interviews, reviewers,
        /// questions and stacks) for the supplied candidate.
        /// </summary>
        public static List<CandidateApplicationResponse> BuildApplications(CandidateResponse candidate)
        {
            var created = candidate.CreatedAt == default ? DateTime.UtcNow.AddDays(-90) : candidate.CreatedAt;

            var senior = new CandidateApplicationResponse
            {
                Id = candidate.Id * 100 + 1,
                CandidateId = candidate.Id,
                PositionId = 41,
                PositionTitle = "Senior .NET Engineer",
                PositionStatus = "Open",
                Status = "Interviewing",
                StartedAt = created.AddDays(3),
                FinalScore = null,
                FinalComments = null,
                Stacks =
                [
                    new StackRef { Id = 1, Name = ".NET" },
                    new StackRef { Id = 2, Name = "Azure" },
                    new StackRef { Id = 3, Name = "SQL Server" }
                ],
                Interviews =
                [
                    new InterviewResponse
                    {
                        Id = 1001, ApplicationId = candidate.Id * 100 + 1,
                        Name = "Screening call", Type = "Screening", Status = "Completed",
                        InterviewerId = 11, InterviewerName = "Ana Pereira",
                        Score = 8.0, Comments = "Strong communicator, relevant background.",
                        CreatedAt = created.AddDays(5), UpdatedAt = created.AddDays(5),
                        Reviewers =
                        [
                            new InterviewReviewerResponse { ReviewerId = 11, ReviewerName = "Ana Pereira", Score = 8.0, Comments = "Great fit for the team.", ReviewedAt = created.AddDays(5) }
                        ]
                    },
                    new InterviewResponse
                    {
                        Id = 1002, ApplicationId = candidate.Id * 100 + 1,
                        Name = "Technical interview", Type = "Technical", Status = "Completed",
                        InterviewerId = 12, InterviewerName = "Diego Alves",
                        Score = 7.5, Comments = "Solid architecture and testing knowledge.",
                        CreatedAt = created.AddDays(12), UpdatedAt = created.AddDays(12),
                        Reviewers =
                        [
                            new InterviewReviewerResponse { ReviewerId = 12, ReviewerName = "Diego Alves", Score = 7.5, Comments = "Good problem solving, minor gaps in async.", ReviewedAt = created.AddDays(12) },
                            new InterviewReviewerResponse { ReviewerId = 15, ReviewerName = "Carla Dias", Score = 8.5, Comments = "Very clean code and clear reasoning.", ReviewedAt = created.AddDays(12) }
                        ],
                        Questions =
                        [
                            new InterviewQuestionResponse { QuestionId = 1, Question = "Explain async/await and the state machine.", Order = 1, Score = 7.0, Stack = ".NET", AcceptableAnswer = "Compiler-generated state machine; captures continuations." },
                            new InterviewQuestionResponse { QuestionId = 2, Question = "Design a rate limiter.", Order = 2, Score = 8.0, Stack = "System Design", AcceptableAnswer = "Token bucket / sliding window with distributed store." },
                            new InterviewQuestionResponse { QuestionId = 3, Question = "Index strategy for a hot query.", Order = 3, Score = 7.5, Stack = "SQL Server", AcceptableAnswer = "Covering index; analyze the execution plan." }
                        ]
                    },
                    new InterviewResponse
                    {
                        Id = 1003, ApplicationId = candidate.Id * 100 + 1,
                        Name = "Final interview", Type = "Final", Status = "Scheduled",
                        InterviewerId = 13, InterviewerName = "Bruno Costa",
                        Score = null, Comments = null,
                        CreatedAt = created.AddDays(20), UpdatedAt = created.AddDays(20)
                    }
                ]
            };

            var lead = new CandidateApplicationResponse
            {
                Id = candidate.Id * 100 + 2,
                CandidateId = candidate.Id,
                PositionId = 27,
                PositionTitle = "Tech Lead",
                PositionStatus = "Closed",
                Status = "Rejected",
                StartedAt = created.AddDays(2),
                FinishedAt = created.AddDays(30),
                FinalScore = 6.2,
                FinalComments = "Strong candidate but limited people-management experience for this role.",
                Stacks =
                [
                    new StackRef { Id = 1, Name = ".NET" },
                    new StackRef { Id = 4, Name = "Leadership" }
                ],
                Interviews =
                [
                    new InterviewResponse
                    {
                        Id = 2001, ApplicationId = candidate.Id * 100 + 2,
                        Name = "Behavioral interview", Type = "Behavioral", Status = "Completed",
                        InterviewerId = 14, InterviewerName = "Elisa Moreira",
                        Score = 6.0, Comments = "Good collaboration examples, less experience leading large teams.",
                        CreatedAt = created.AddDays(10), UpdatedAt = created.AddDays(10),
                        Reviewers =
                        [
                            new InterviewReviewerResponse { ReviewerId = 14, ReviewerName = "Elisa Moreira", Score = 6.0, Comments = "Would benefit from more leadership exposure.", ReviewedAt = created.AddDays(10) }
                        ]
                    }
                ]
            };

            var platform = new CandidateApplicationResponse
            {
                Id = candidate.Id * 100 + 3,
                CandidateId = candidate.Id,
                PositionId = 52,
                PositionTitle = "Platform Engineer",
                PositionStatus = "Open",
                Status = "Applied",
                StartedAt = created.AddDays(1),
                Stacks =
                [
                    new StackRef { Id = 2, Name = "Azure" },
                    new StackRef { Id = 5, Name = "Kubernetes" }
                ],
                Interviews = []
            };

            return [senior, lead, platform];
        }

        /// <summary>
        /// Synthesizes a chronological recruitment timeline from the candidate and
        /// their applications. Reused by the page when only the timeline endpoint is
        /// missing, so the timeline stays consistent with the loaded applications.
        /// </summary>
        public static List<TimelineEvent> BuildTimeline(
            CandidateResponse candidate,
            IReadOnlyCollection<CandidateApplicationResponse> applications)
        {
            var events = new List<TimelineEvent>
            {
                new()
                {
                    Timestamp = candidate.CreatedAt,
                    EventType = "candidate_created",
                    Title = "Candidate created",
                    Description = $"{candidate.FullName} was added to the pipeline.",
                    Status = "Created"
                }
            };

            foreach (var app in applications)
            {
                if (app.StartedAt is { } started)
                {
                    events.Add(new TimelineEvent
                    {
                        Timestamp = started,
                        EventType = "application_started",
                        Title = "Application started",
                        Description = $"Applied to {app.PositionTitle}.",
                        Position = app.PositionTitle,
                        Status = app.Status
                    });
                }

                foreach (var interview in app.Interviews)
                {
                    if (interview.CreatedAt is { } scheduled)
                    {
                        events.Add(new TimelineEvent
                        {
                            Timestamp = scheduled,
                            EventType = "interview_created",
                            Title = $"{interview.Type} interview scheduled",
                            Description = interview.InterviewerName is { Length: > 0 } name
                                ? $"{interview.Name} with {name}."
                                : interview.Name,
                            Position = app.PositionTitle,
                            Interview = interview.Name,
                            Status = interview.Status
                        });
                    }

                    if (interview.IsCompleted && interview.UpdatedAt is { } completed)
                    {
                        events.Add(new TimelineEvent
                        {
                            Timestamp = completed,
                            EventType = "interview_completed",
                            Title = $"{interview.Type} interview completed",
                            Description = interview.Score is { } score
                                ? $"{interview.Name} scored {score:0.0}."
                                : interview.Name,
                            Position = app.PositionTitle,
                            Interview = interview.Name,
                            Status = "Completed"
                        });
                    }
                }

                if (app.FinishedAt is { } finished)
                {
                    events.Add(new TimelineEvent
                    {
                        Timestamp = finished,
                        EventType = "application_finished",
                        Title = "Application finished",
                        Description = app.FinalComments is { Length: > 0 } comments
                            ? comments
                            : $"{app.PositionTitle} application closed.",
                        Position = app.PositionTitle,
                        Status = app.Status
                    });
                }
            }

            return events
                .OrderBy(e => e.Timestamp)
                .ToList();
        }
    }
}
