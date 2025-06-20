using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

namespace Cosmetics.DTO.AIScanner
{
    public class AIScannerDto
    {
    }
    public class FileUploadRequest 
    { 
        public IList? Files { get; set; } 
    }
    public class FileInfo
    {
        public string? ContentType { get; set; }
        public string? FileName { get; set; }
        public long FileSize { get; set; }
    }

    public class FileUploadResponse
    {
        public int Status { get; set; }
        public FileUploadResult? Result { get; set; }
    }

    public class FileUploadResult
    {
        public List<FileDTO>? Files { get; set; }
    }

    public class FileDTO
    {
        public string? ContentType { get; set; }
        public string? FileName { get; set; }
        public string? FileId { get; set; }
        public List<UploadRequest>? Requests { get; set; }
    }

    public class UploadRequest
    {
        public Dictionary<string, string>? Headers { get; set; }
        public string? Url { get; set; }
        public string? Method { get; set; }
    }

    public class RunTaskRequest
    {
        public int RequestId { get; set; }
        public Payload? Payload { get; set; }
    }

    public class Payload
    {
        public FileSets? FileSets { get; set; }
        public List<Action>? Actions { get; set; }
    }

    public class FileSets
    {
        public List<string>? SrcIds { get; set; }
    }

    public class Action
    {
        public int Id { get; set; }
        public Params? Params { get; set; }
    }

    public class Params
    {
        public List<string>? DstActions { get; set; }
    }

    public class RunTaskResponse
    {
        public int Status { get; set; }
        public TaskResult? Result { get; set; }
    }

    public class TaskResult
    {
        public string? TaskId { get; set; }
    }

    public class CheckStatusResponse
    {
        public int Status { get; set; }
        public StatusResult? Result { get; set; }
    }

    public class StatusResult
    {
        public int PollingInterval { get; set; }
        public string? Status { get; set; }
        public string? Error { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ActionResult>? Results { get; set; }
    }

    public class ActionResult
    {
        public int Id { get; set; }
        public List<Data>? Data { get; set; }
    }

    public class Data
    {
        public string? Url { get; set; }
    }
}
