using System.Text.Json;
using TH_ExamGen.Core.Config;
using TH_ExamGen.Core.Curriculum;
using TH_ExamGen.Core.MathModels;
using TH_ExamGen.Core.VietModels;
using TH_ExamGen.Services;

var builder = WebApplication.CreateBuilder(args);

// Port from environment variable (Render.com passes PORT)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// Helper to get effective settings (file + env vars + optional request overrides)
AppSettings GetEffectiveSettings(AppSettings? clientOverrides = null)
{
    var settings = AppSettings.Load();

    var envGemini = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (!string.IsNullOrWhiteSpace(envGemini)) settings.GeminiApiKey = envGemini;

    var envDeepSeek = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
    if (!string.IsNullOrWhiteSpace(envDeepSeek)) settings.DeepSeekApiKey = envDeepSeek;

    var envProvider = Environment.GetEnvironmentVariable("AI_PROVIDER");
    if (!string.IsNullOrWhiteSpace(envProvider)) settings.AiProvider = envProvider;

    if (clientOverrides != null)
    {
        if (!string.IsNullOrWhiteSpace(clientOverrides.TenUBND)) settings.TenUBND = clientOverrides.TenUBND;
        if (!string.IsNullOrWhiteSpace(clientOverrides.TenTruong)) settings.TenTruong = clientOverrides.TenTruong;
        if (!string.IsNullOrWhiteSpace(clientOverrides.NamHoc)) settings.NamHoc = clientOverrides.NamHoc;
        if (!string.IsNullOrWhiteSpace(clientOverrides.HocKy)) settings.HocKy = clientOverrides.HocKy;
        if (!string.IsNullOrWhiteSpace(clientOverrides.GeminiApiKey)) settings.GeminiApiKey = clientOverrides.GeminiApiKey;
        if (!string.IsNullOrWhiteSpace(clientOverrides.DeepSeekApiKey)) settings.DeepSeekApiKey = clientOverrides.DeepSeekApiKey;
        if (!string.IsNullOrWhiteSpace(clientOverrides.AiProvider)) settings.AiProvider = clientOverrides.AiProvider;
        if (!string.IsNullOrWhiteSpace(clientOverrides.GeminiModel)) settings.GeminiModel = clientOverrides.GeminiModel;
    }

    return settings;
}

// 1. Health check for Render
app.MapGet("/health", () => Results.Ok(new { status = "healthy", serverTime = DateTime.UtcNow }));
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", serverTime = DateTime.UtcNow }));

// 2. Settings endpoints
app.MapGet("/api/settings", () =>
{
    var s = GetEffectiveSettings();
    return Results.Ok(new
    {
        s.AiProvider,
        s.ActiveModel,
        s.TenUBND,
        s.TenTruong,
        s.NamHoc,
        s.HocKy,
        HasGeminiKey = !string.IsNullOrWhiteSpace(s.GeminiApiKey),
        HasDeepSeekKey = !string.IsNullOrWhiteSpace(s.DeepSeekApiKey),
        // Masked keys for display
        GeminiApiKey = string.IsNullOrWhiteSpace(s.GeminiApiKey) ? "" : "******" + s.GeminiApiKey[^Math.Min(4, s.GeminiApiKey.Length)..],
        DeepSeekApiKey = string.IsNullOrWhiteSpace(s.DeepSeekApiKey) ? "" : "******" + s.DeepSeekApiKey[^Math.Min(4, s.DeepSeekApiKey.Length)..]
    });
});

app.MapPost("/api/settings", (AppSettings newSettings) =>
{
    var current = AppSettings.Load();
    if (!string.IsNullOrWhiteSpace(newSettings.TenUBND)) current.TenUBND = newSettings.TenUBND;
    if (!string.IsNullOrWhiteSpace(newSettings.TenTruong)) current.TenTruong = newSettings.TenTruong;
    if (!string.IsNullOrWhiteSpace(newSettings.NamHoc)) current.NamHoc = newSettings.NamHoc;
    if (!string.IsNullOrWhiteSpace(newSettings.HocKy)) current.HocKy = newSettings.HocKy;
    if (!string.IsNullOrWhiteSpace(newSettings.AiProvider)) current.AiProvider = newSettings.AiProvider;
    if (!string.IsNullOrWhiteSpace(newSettings.GeminiModel)) current.GeminiModel = newSettings.GeminiModel;
    if (!string.IsNullOrWhiteSpace(newSettings.GeminiApiKey) && !newSettings.GeminiApiKey.StartsWith("******"))
        current.GeminiApiKey = newSettings.GeminiApiKey;
    if (!string.IsNullOrWhiteSpace(newSettings.DeepSeekApiKey) && !newSettings.DeepSeekApiKey.StartsWith("******"))
        current.DeepSeekApiKey = newSettings.DeepSeekApiKey;

    current.Save();
    return Results.Ok(new { message = "Cài đặt đã được lưu thành công!" });
});

// 3. Curriculum / Matrix
app.MapGet("/api/curriculum/viet/matrix", (int grade, string period) =>
{
    grade = (grade == 5) ? 5 : 4;
    period = string.IsNullOrWhiteSpace(period) ? "Cuối Học kỳ II" : period;
    var list = VietCurriculumBank.GetMatrixByGradeAndPeriod(grade, period);
    var defaultTheme = VietCurriculumBank.GetDefaultTheme(grade, period);
    var confirmedTopics = CurriculumScopePolicy.GetConfirmedVietReadingTopics(grade, period)
        .Select(x => x.ChuDe.Replace("Chủ điểm", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(' ', '"'))
        .Distinct()
        .ToList();

    return Results.Ok(new
    {
        grade,
        period,
        defaultTheme,
        confirmedTopics,
        items = list
    });
});

app.MapGet("/api/curriculum/math/matrix", (int grade, string period) =>
{
    grade = (grade == 5) ? 5 : 4;
    period = string.IsNullOrWhiteSpace(period) ? "Cuối Học kỳ II" : period;
    var list = MathCurriculumBank.GetMatrixByGradeAndPeriod(grade, period);

    return Results.Ok(new
    {
        grade,
        period,
        items = list
    });
});

// 4. AI Generator Endpoints
app.MapPost("/api/ai/viet/audit-matrix", async (AuditVietMatrixDto req) =>
{
    try
    {
        var settings = GetEffectiveSettings(req.Settings);
        var gemini = new GeminiService();
        var report = await gemini.AuditVietMatrixAsync(settings, req.Grade, req.Period, req.Matrix);
        return Results.Ok(new { report });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/ai/math/audit-matrix", async (AuditMathMatrixDto req) =>
{
    try
    {
        var settings = GetEffectiveSettings(req.Settings);
        var gemini = new GeminiService();
        var report = await gemini.AuditMathMatrixAsync(settings, req.Grade, req.Period, req.Matrix);
        return Results.Ok(new { report });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/ai/viet/generate-exam", async (GenerateVietExamDto req, CancellationToken ct) =>
{
    try
    {
        var settings = GetEffectiveSettings(req.Settings);
        var gemini = new GeminiService();
        var paper = await gemini.GenerateVietExamAsync(
            settings,
            req.Grade,
            req.DurationMinutes <= 0 ? 35 : req.DurationMinutes,
            req.Matrix,
            req.Passage,
            req.Theme,
            req.Period,
            ct);

        return Results.Ok(paper);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/ai/math/generate-exam", async (GenerateMathExamDto req, CancellationToken ct) =>
{
    try
    {
        var settings = GetEffectiveSettings(req.Settings);
        var gemini = new GeminiService();
        var paper = await gemini.GenerateMathExamAsync(
            settings,
            req.Grade,
            req.DurationMinutes <= 0 ? 40 : req.DurationMinutes,
            req.Matrix,
            req.Period,
            ct);

        return Results.Ok(paper);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// 5. Word Export Endpoints (Direct Download)
app.MapPost("/api/export/viet/matrix", async (ExportVietMatrixDto req) =>
{
    var tempFile = Path.Combine(Path.GetTempPath(), $"viet_matrix_{Guid.NewGuid():N}.docx");
    try
    {
        var settings = GetEffectiveSettings(req.Settings);
        WordExportVietService.ExportVietMatrixOnlyToDocx(req.Matrix, settings, req.Grade, tempFile, req.Period);
        byte[] bytes = await File.ReadAllBytesAsync(tempFile);
        string filename = $"MaTran_DacTa_TiengViet_Lop{req.Grade}_{req.Period.Replace(" ", "_")}.docx";
        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", filename);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ExportVietMatrix] Error: {ex}");
        return Results.BadRequest(new { error = ex.Message });
    }
    finally
    {
        if (File.Exists(tempFile)) { try { File.Delete(tempFile); } catch { } }
    }
});

app.MapPost("/api/export/viet/exam", async (ExportVietExamDto req) =>
{
    var tempFile = Path.Combine(Path.GetTempPath(), $"viet_exam_{Guid.NewGuid():N}.docx");
    try
    {
        var settings = GetEffectiveSettings(req.Settings);
        WordExportVietService.ExportVietExamOnlyToDocx(req.Paper, settings, tempFile, req.SkipValidation);
        byte[] bytes = await File.ReadAllBytesAsync(tempFile);
        string filename = $"DeThi_TiengViet_Lop{req.Paper.KhoiLop}_{req.Paper.Period.Replace(" ", "_")}.docx";
        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", filename);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    finally
    {
        if (File.Exists(tempFile)) { try { File.Delete(tempFile); } catch { } }
    }
});

app.MapPost("/api/export/viet/full", async (ExportVietExamDto req) =>
{
    var tempFile = Path.Combine(Path.GetTempPath(), $"viet_full_{Guid.NewGuid():N}.docx");
    try
    {
        var settings = GetEffectiveSettings(req.Settings);
        WordExportVietService.ExportVietFullPackageToDocx(req.Paper, settings, tempFile, req.SkipValidation);
        byte[] bytes = await File.ReadAllBytesAsync(tempFile);
        string filename = $"TronBo_DeThi_Va_DapAn_TiengViet_Lop{req.Paper.KhoiLop}_{req.Paper.Period.Replace(" ", "_")}.docx";
        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", filename);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    finally
    {
        if (File.Exists(tempFile)) { try { File.Delete(tempFile); } catch { } }
    }
});

app.MapPost("/api/export/math/matrix", async (ExportMathMatrixDto req) =>
{
    var tempFile = Path.Combine(Path.GetTempPath(), $"math_matrix_{Guid.NewGuid():N}.docx");
    try
    {
        var settings = GetEffectiveSettings(req.Settings);
        WordExportMathService.ExportMathMatrixOnlyToDocx(req.Matrix, settings, req.Grade, tempFile, req.Period);
        byte[] bytes = await File.ReadAllBytesAsync(tempFile);
        string filename = $"MaTran_DacTa_Toan_Lop{req.Grade}_{req.Period.Replace(" ", "_")}.docx";
        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", filename);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    finally
    {
        if (File.Exists(tempFile)) { try { File.Delete(tempFile); } catch { } }
    }
});

app.MapPost("/api/export/math/exam", async (ExportMathExamDto req) =>
{
    var tempFile = Path.Combine(Path.GetTempPath(), $"math_exam_{Guid.NewGuid():N}.docx");
    try
    {
        var settings = GetEffectiveSettings(req.Settings);
        WordExportMathService.ExportMathExamOnlyToDocx(req.Paper, settings, tempFile, req.SkipValidation);
        byte[] bytes = await File.ReadAllBytesAsync(tempFile);
        string filename = $"DeThi_Toan_Lop{req.Paper.KhoiLop}_{req.Paper.Period.Replace(" ", "_")}.docx";
        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", filename);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    finally
    {
        if (File.Exists(tempFile)) { try { File.Delete(tempFile); } catch { } }
    }
});

app.MapPost("/api/export/math/full", async (ExportMathExamDto req) =>
{
    var tempFile = Path.Combine(Path.GetTempPath(), $"math_full_{Guid.NewGuid():N}.docx");
    try
    {
        var settings = GetEffectiveSettings(req.Settings);
        WordExportMathService.ExportMathFullPackageToDocx(req.Paper, settings, tempFile, req.SkipValidation);
        byte[] bytes = await File.ReadAllBytesAsync(tempFile);
        string filename = $"TronBo_DeThi_Va_DapAn_Toan_Lop{req.Paper.KhoiLop}_{req.Paper.Period.Replace(" ", "_")}.docx";
        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", filename);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    finally
    {
        if (File.Exists(tempFile)) { try { File.Delete(tempFile); } catch { } }
    }
});

app.Run();

// DTO classes
public class AuditVietMatrixDto
{
    public int Grade { get; set; } = 4;
    public string Period { get; set; } = "Cuối Học kỳ II";
    public List<VietMatrixItem> Matrix { get; set; } = new();
    public AppSettings? Settings { get; set; }
}

public class AuditMathMatrixDto
{
    public int Grade { get; set; } = 4;
    public string Period { get; set; } = "Cuối Học kỳ II";
    public List<MathMatrixItem> Matrix { get; set; } = new();
    public AppSettings? Settings { get; set; }
}

public class GenerateVietExamDto
{
    public int Grade { get; set; } = 4;
    public string Period { get; set; } = "Cuối Học kỳ II";
    public int DurationMinutes { get; set; } = 35;
    public string Theme { get; set; } = string.Empty;
    public List<VietMatrixItem> Matrix { get; set; } = new();
    public ReadingPassage? Passage { get; set; }
    public AppSettings? Settings { get; set; }
}

public class GenerateMathExamDto
{
    public int Grade { get; set; } = 4;
    public string Period { get; set; } = "Cuối Học kỳ II";
    public int DurationMinutes { get; set; } = 40;
    public List<MathMatrixItem> Matrix { get; set; } = new();
    public AppSettings? Settings { get; set; }
}

public class ExportVietMatrixDto
{
    public int Grade { get; set; } = 4;
    public string Period { get; set; } = "Cuối Học kỳ II";
    public List<VietMatrixItem> Matrix { get; set; } = new();
    public AppSettings? Settings { get; set; }
}

public class ExportVietExamDto
{
    public VietExamPaper Paper { get; set; } = new();
    public bool SkipValidation { get; set; } = false;
    public AppSettings? Settings { get; set; }
}

public class ExportMathMatrixDto
{
    public int Grade { get; set; } = 4;
    public string Period { get; set; } = "Cuối Học kỳ II";
    public List<MathMatrixItem> Matrix { get; set; } = new();
    public AppSettings? Settings { get; set; }
}

public class ExportMathExamDto
{
    public MathExamPaper Paper { get; set; } = new();
    public bool SkipValidation { get; set; } = false;
    public AppSettings? Settings { get; set; }
}
