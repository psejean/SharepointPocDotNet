using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace MyFunctionApp
{
    public static class GetTeacherEmailListFunction
    {
        static GetTeacherEmailListFunction()
        {
            // Set the allowed logon version globally when the class is initialized
            OracleConfiguration.SqlNetAllowedLogonVersionClient = OracleAllowedLogonVersionClient.Version8;
        }

        [Function("GetTeacherEmailListFunction")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, // AuthorizationLevel is set to Anonymous
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("GetTeacherEmailListFunction");
            logger.LogInformation("GetTeacherEmailListFunction triggered.");

            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string studentId = query["studentId"];
            if (string.IsNullOrEmpty(studentId))
            {
                var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Please pass a studentId on the query string.");
                return badRequestResponse;
            }

            try
            {
                // Oracle connection string from environment variables
                string connectionString = Environment.GetEnvironmentVariable("OracleConnectionString");

                using (var connection = new OracleConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "TEACHER_EMAIL_LIST";
                        command.CommandType = System.Data.CommandType.StoredProcedure;

                        // Input parameter
                        command.Parameters.Add("studentId", OracleDbType.Varchar2).Value = studentId;

                        // Output parameter, increased size to 200 characters
                        var outputParam = new OracleParameter("out_param", OracleDbType.Varchar2, 200);
                        outputParam.Direction = System.Data.ParameterDirection.Output;
                        command.Parameters.Add(outputParam);

                        await command.ExecuteNonQueryAsync();

                        string result = outputParam.Value.ToString();

                        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                        await response.WriteStringAsync($"{result}");
                        return response;
                    }
                }
            }
            catch (OracleException ex)
            {
                logger.LogError($"Oracle DB error: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Database error occurred.");
                return errorResponse;
            }
            catch (Exception ex)
            {
                logger.LogError($"General error: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An error occurred.");
                return errorResponse;
            }
        }
    }
}
