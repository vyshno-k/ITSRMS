namespace ITServiceRequest.Api.DTOs;

public class AddCommentDto
{
    public int? EmployeeId { get; set; }

    public string CommentText { get; set; } = "";
}