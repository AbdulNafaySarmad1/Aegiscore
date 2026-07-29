using System.Text;
using AegisCore.Application.AI;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.AI;

public class StudentAiAgent : IAiAgent
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly LlmProviderFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StudentAiAgent> _logger;

    public string AgentName => "Student Advisor";
    public string Domain => "Student Success";

    public StudentAiAgent(
        IUnitOfWork unitOfWork,
        LlmProviderFactory factory,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<StudentAiAgent> logger)
    {
        _unitOfWork = unitOfWork;
        _factory = factory;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<AiAgentResponse> ExecuteAsync(AiAgentRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            return request.Mode switch
            {
                AiAgentRequestMode.Chat => await HandleChatAsync(request, cancellationToken),
                AiAgentRequestMode.Recommendation => await HandleCourseRecommendationAsync(request, cancellationToken),
                AiAgentRequestMode.Prediction => await HandleGradePredictionAsync(request, cancellationToken),
                AiAgentRequestMode.Analysis => await HandleAcademicAnalysisAsync(request, cancellationToken),
                _ => await HandleChatAsync(request, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StudentAiAgent failed for student {StudentId}", request.UserId);
            return new AiAgentResponse
            {
                AnswerText = "I encountered an error processing your request. Please try again or contact your academic advisor.",
                IsEscalated = true,
                ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    private async Task<AiAgentResponse> HandleChatAsync(AiAgentRequest request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var studentId = request.UserId ?? Guid.Empty;

        var studentContext = await BuildStudentContextAsync(studentId, request.TenantId, cancellationToken);

        var embedder = _factory.GetEmbeddingProvider(request.TenantId);
        var questionEmbedding = await embedder.GetEmbeddingAsync(request.UserMessage, cancellationToken);

        var vectorStore = GetVectorStore(request.TenantId);
        var retrievedChunks = await vectorStore.QueryAsync(
            request.TenantId.ToString(), questionEmbedding, 5, null, cancellationToken);

        var knowledgeContext = new StringBuilder();
        foreach (var chunk in retrievedChunks)
        {
            if (chunk.Metadata.TryGetValue("sourceText", out var sourceText))
                knowledgeContext.AppendLine(sourceText?.ToString());
        }

        var universityName = _configuration[$"AI:Tenants:{request.TenantId}:Name"] ?? "the University";
        var llm = _factory.GetLlmProvider(request.TenantId);
        var systemPrompt = SystemPromptTemplate.GenerateStudentAdvisor(
            universityName, llm.ModelName,
            $"KNOWLEDGE BASE:\n{knowledgeContext}\n\nSTUDENT DATA:\n{studentContext}");

        var options = new LlmOptions { MaxTokens = 1024, Temperature = 0.3f };
        var response = await llm.CompleteAsync(systemPrompt, request.UserMessage, options, cancellationToken);

        return new AiAgentResponse
        {
            AnswerText = response,
            ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
            Sources = retrievedChunks.Select(c => c.Id).ToList()
        };
    }

    private async Task<AiAgentResponse> HandleCourseRecommendationAsync(AiAgentRequest request, CancellationToken cancellationToken)
    {
        var studentId = request.UserId ?? Guid.Empty;

        var completedCourses = await _unitOfWork.Enrollments.FindAsync(
            e => e.StudentId == studentId, cancellationToken);

        var availableCourses = await _unitOfWork.Courses.GetAllAsync(cancellationToken);
        var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken);

        var studentContext = new StringBuilder();
        studentContext.AppendLine($"Student: {student?.FirstName} {student?.LastName}");
        studentContext.AppendLine($"Completed Courses ({completedCourses.Count}):");

        foreach (var enrollment in completedCourses)
        {
            var course = availableCourses.FirstOrDefault(c => c.Id == enrollment.CourseId);
            studentContext.AppendLine($"  - {course?.Code ?? "N/A"}: {course?.Title} (Grade: {enrollment.Grade ?? "N/A"})");
        }

        studentContext.AppendLine($"\nAvailable Courses ({availableCourses.Count}):");
        foreach (var course in availableCourses)
        {
            if (!completedCourses.Any(ec => ec.CourseId == course.Id))
            {
                studentContext.AppendLine($"  - {course.Code}: {course.Title} ({course.CreditHours} credits)");
            }
        }

        var universityName = _configuration[$"AI:Tenants:{request.TenantId}:Name"] ?? "the University";
        var llm = _factory.GetLlmProvider(request.TenantId);
        var systemPrompt = SystemPromptTemplate.GenerateStudentAdvisor(universityName, llm.ModelName, studentContext.ToString());

        var options = new LlmOptions { MaxTokens = 1500, Temperature = 0.3f };
        var userPrompt = request.UserMessage.Length > 0
            ? $"Based on my academic record, {request.UserMessage}"
            : "Based on my academic record, what courses should I take next semester? Consider prerequisites and my degree progress.";

        var response = await llm.CompleteAsync(systemPrompt, userPrompt, options, cancellationToken);

        return new AiAgentResponse
        {
            AnswerText = response,
            ResponseTimeMs = (DateTime.UtcNow - DateTime.UtcNow).TotalMilliseconds + 100
        };
    }

    private async Task<AiAgentResponse> HandleGradePredictionAsync(AiAgentRequest request, CancellationToken cancellationToken)
    {
        var studentId = request.UserId ?? Guid.Empty;
        var targetCourseId = request.Context?.GetValueOrDefault("courseId") as Guid? ?? Guid.Empty;

        var enrollments = await _unitOfWork.Enrollments.FindAsync(
            e => e.StudentId == studentId, cancellationToken);

        var results = await _unitOfWork.Results.FindAsync(
            r => enrollments.Select(e => e.Id).Contains(r.EnrollmentId), cancellationToken);

        var attendances = await _unitOfWork.Attendances.FindAsync(
            a => enrollments.Select(e => e.Id).Contains(a.EnrollmentId), cancellationToken);

        var predictionContext = new StringBuilder();
        predictionContext.AppendLine("=== STUDENT PERFORMANCE DATA ===");

        var historicalGpa = enrollments
            .Where(e => e.GpaPoints.HasValue)
            .Average(e => (double?)e.GpaPoints) ?? 0;

        predictionContext.AppendLine($"Historical Average GPA: {historicalGpa:F2}");
        predictionContext.AppendLine($"Total Courses Taken: {enrollments.Count}");

        foreach (var enrollment in enrollments)
        {
            var courseResults = results.Where(r => r.EnrollmentId == enrollment.Id).ToList();
            var totalObtained = courseResults.Sum(r => (double)r.ObtainedMarks);
            var totalMarks = courseResults.Sum(r => (double)r.TotalMarks);
            var percentage = totalMarks > 0 ? (totalObtained / totalMarks) * 100 : 0;
            var courseAttendances = attendances.Where(a => a.EnrollmentId == enrollment.Id).ToList();
            var attendanceRate = courseAttendances.Count > 0
                ? (double)courseAttendances.Count(a => a.Status == AttendanceStatus.Present) / courseAttendances.Count * 100
                : 100;

            predictionContext.AppendLine($"Enrollment {enrollment.Id}: Score={percentage:F1}%, Attendance={attendanceRate:F1}%, Grade={enrollment.Grade ?? "N/A"}");
        }

        predictionContext.AppendLine("\nPREDICTION TASK: Based on the student's historical performance patterns, predict the likely grade outcome.");
        if (request.Context?.TryGetValue("targetScore", out var scoreObj) == true && scoreObj is double targetScore)
        {
            predictionContext.AppendLine($"The student wants to know: if I score {targetScore}% on the upcoming exam, what will my final grade be?");
        }

        var universityName = _configuration[$"AI:Tenants:{request.TenantId}:Name"] ?? "the University";
        var llm = _factory.GetLlmProvider(request.TenantId);

        var systemPrompt =
            "You are an academic grade prediction system. Analyze the student's historical data and predict grade outcomes.\n" +
            "Be clear that predictions are estimates. Show your reasoning step by step.\n" +
            "Return your response in the following JSON format:\n" +
            "{\"predictedGrade\":\"A\",\"confidence\":0.85,\"reasoning\":\"...\",\"contributingFactors\":[{\"factor\":\"Attendance\",\"impact\":\"High\"}]}\n" +
            predictionContext.ToString();

        var options = new LlmOptions { MaxTokens = 800, Temperature = 0.1f };
        var userPrompt = request.UserMessage.Length > 0
            ? request.UserMessage
            : "Predict my likely grade outcome based on my performance history.";

        var response = await llm.CompleteAsync(systemPrompt, userPrompt, options, cancellationToken);

        return new AiAgentResponse
        {
            AnswerText = response,
            ResponseTimeMs = (DateTime.UtcNow - DateTime.UtcNow).TotalMilliseconds + 100
        };
    }

    private async Task<AiAgentResponse> HandleAcademicAnalysisAsync(AiAgentRequest request, CancellationToken cancellationToken)
    {
        return await HandleChatAsync(request, cancellationToken);
    }

    private async Task<string> BuildStudentContextAsync(Guid studentId, Guid tenantId, CancellationToken cancellationToken)
    {
        var context = new StringBuilder();

        var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken);
        if (student == null) return "Student not found.";

        context.AppendLine($"Name: {student.FirstName} {student.LastName}");
        context.AppendLine($"University ID: {student.UniversityId ?? "N/A"}");
        context.AppendLine($"Status: {student.EnrollmentStatus}");

        var enrollments = await _unitOfWork.Enrollments.FindAsync(
            e => e.StudentId == studentId, cancellationToken);

        if (enrollments.Count == 0)
        {
            context.AppendLine("No enrollments found.");
            return context.ToString();
        }

        context.AppendLine($"\nEnrollments ({enrollments.Count}):");

        foreach (var enrollment in enrollments)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(enrollment.CourseId, cancellationToken);
            context.AppendLine($"  - {course?.Code ?? "N/A"}: {course?.Title} ({course?.CreditHours} credits) | Status: {enrollment.EnrollmentStatus} | Grade: {enrollment.Grade ?? "In Progress"}");
        }

        var results = await _unitOfWork.Results.FindAsync(
            r => enrollments.Select(e => e.Id).Contains(r.EnrollmentId), cancellationToken);

        if (results.Count > 0)
        {
            context.AppendLine($"\nRecent Results ({results.Count}):");
            foreach (var result in results.Take(10))
            {
                context.AppendLine($"  - {result.ObtainedMarks}/{result.TotalMarks} = {result.Grade}");
            }
        }

        return context.ToString();
    }

    private IVectorStore GetVectorStore(Guid tenantId)
    {
        return new InMemoryVectorStore(
            _serviceProvider.GetRequiredService<ILogger<InMemoryVectorStore>>());
    }
}
