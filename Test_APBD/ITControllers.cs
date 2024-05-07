using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

using System.Data;

namespace Test_APBD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ITControllers : ControllerBase
    {
        private readonly string _connectionString;

        public ITControllers(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DataString");
        }

        [HttpGet("{id}")]
        public IActionResult GetTasksByID(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
                SELECT
                    tm.IdTeamMember,
                    tm.FirstName,
                    tm.LastName,
                    tm.Email,
                    t1.Name AS AssignedTaskName,
                    t1.Description AS AssignedTaskDescription,
                    t1.Deadline AS AssignedTaskDeadline,
                    p1.Name AS AssignedTaskProjectName,
                    tt1.Name AS AssignedTaskTypeName,
                    t2.Name AS CreatedTaskName,
                    t2.Description AS CreatedTaskDescription,
                    t2.Deadline AS CreatedTaskDeadline,
                    p2.Name AS CreatedTaskProjectName,
                    tt2.Name AS CreatedTaskTypeName
                FROM
                    TeamMember tm
                    LEFT JOIN Task t1 ON tm.IdTeamMember = t1.IdAssignedTo
                    LEFT JOIN Project p1 ON t1.IdProject = p1.IdProject
                    LEFT JOIN TaskType tt1 ON t1.IdTaskType = tt1.IdTaskType
                    LEFT JOIN Task t2 ON tm.IdTeamMember = t2.IdCreator
                    LEFT JOIN Project p2 ON t2.IdProject = p2.IdProject
                    LEFT JOIN TaskType tt2 ON t2.IdTaskType = tt2.IdTaskType
                WHERE
                    tm.IdTeamMember = @Id
                ORDER BY
                    t1.Deadline DESC, t2.Deadline DESC;";

            var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            var dataSet = new DataSet();
            var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataSet);

            if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                return NotFound("Team member not found");

            var result = new Dictionary<string, object>();

            var teamMemberRow = dataSet.Tables[0].Rows[0];
            result["IdTeamMember"] = teamMemberRow["IdTeamMember"];
            result["FirstName"] = teamMemberRow["FirstName"];
            result["LastName"] = teamMemberRow["LastName"];
            result["Email"] = teamMemberRow["Email"];

            var assignedTasks = new List<Dictionary<string, object>>();
            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                if (!row.IsNull("AssignedTaskName"))
                {
                    var taskDict = new Dictionary<string, object>
                    {
                        ["Name"] = row["AssignedTaskName"],
                        ["Description"] = row["AssignedTaskDescription"],
                        ["Deadline"] = row["AssignedTaskDeadline"],
                        ["ProjectName"] = row["AssignedTaskProjectName"],
                        ["TaskTypeName"] = row["AssignedTaskTypeName"]
                    };
                    assignedTasks.Add(taskDict);
                }
            }
            result["AssignedTasks"] = assignedTasks;

            var createdTasks = new List<Dictionary<string, object>>();
            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                if (!row.IsNull("CreatedTaskName"))
                {
                    var taskDict = new Dictionary<string, object>
                    {
                        ["Name"] = row["CreatedTaskName"],
                        ["Description"] = row["CreatedTaskDescription"],
                        ["Deadline"] = row["CreatedTaskDeadline"],
                        ["ProjectName"] = row["CreatedTaskProjectName"],
                        ["TaskTypeName"] = row["CreatedTaskTypeName"]
                    };
                    createdTasks.Add(taskDict);
                }
            }
            result["CreatedTasks"] = createdTasks;

            return Ok(result);
        }

        [HttpPost]
        public IActionResult AddNewTask([FromBody] TaskDto taskDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    string addTask = @"
                            INSERT INTO Task (IdProject, IdTaskType, Name, Description, Deadline, IdAssignedTo, IdCreator)
                            VALUES (@IdProject, @IdTaskType, @Name, @Description, @Deadline, @IdAssignedTo, @IdCreator);
                            SELECT SCOPE_IDENTITY();";

                    var command = new SqlCommand(addTask, connection, transaction);
                    command.Parameters.AddWithValue("@IdProject", taskDto.IdProject);
                    command.Parameters.AddWithValue("@IdTaskType", taskDto.IdTaskType);
                    command.Parameters.AddWithValue("@Name", taskDto.Name);
                    command.Parameters.AddWithValue("@Description", taskDto.Description);
                    command.Parameters.AddWithValue("@Deadline", taskDto.Deadline);
                    command.Parameters.AddWithValue("@IdAssignedTo", taskDto.IdAssignedTo);
                    command.Parameters.AddWithValue("@IdCreator", taskDto.IdCreator);

                    var newTaskId = Convert.ToInt32(command.ExecuteScalar());

                    transaction.Commit();

                    return Ok(new { Id = newTaskId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, $"An error occurred: {ex.Message}");
                }
            }
        }
    }


}
