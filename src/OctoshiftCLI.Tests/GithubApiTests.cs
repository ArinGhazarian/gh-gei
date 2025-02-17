using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using Octoshift.Models;
using OctoshiftCLI.Extensions;
using Xunit;

namespace OctoshiftCLI.Tests
{
    public class GithubApiTests
    {
        private const string Api_Url = $"https://api.github.com";
        private readonly RetryPolicy _retryPolicy = new RetryPolicy(TestHelpers.CreateMock<OctoLogger>().Object);

        [Fact]
        public async Task AddAutoLink_Calls_The_Right_Endpoint_With_Payload()
        {
            // Arrange
            const string org = "ORG";
            const string repo = "REPO";
            const string adoOrg = "ADO_ORG";
            const string adoTeamProject = "ADO_TEAM_PROJECT";

            var keyPrefix = "AB#";
            var urlTemplate = $"https://dev.azure.com/{adoOrg}/{adoTeamProject}/_workitems/edit/<num>/";

            var url = $"https://api.github.com/repos/{org}/{repo}/autolinks";

            var payload = new
            {
                key_prefix = keyPrefix,
                url_template = urlTemplate.Replace(" ", "%20")
            };

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            await githubApi.AddAutoLink(org, repo, keyPrefix, urlTemplate);

            // Assert
            githubClientMock.Verify(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload.ToJson())));
        }

        [Fact]
        public async Task AddAutoLink_Replaces_Spaces_In_Url_Tempalte()
        {
            // Arrange
            const string org = "ORG";
            const string repo = "REPO";
            const string adoOrg = "ADO_ORG";
            const string adoTeamProject = "ADO TEAM PROJECT";

            var keyPrefix = "AB#";
            var urlTemplate = $"https://dev.azure.com/{adoOrg}/{adoTeamProject}/_workitems/edit/<num>/";

            var url = $"https://api.github.com/repos/{org}/{repo}/autolinks";

            var payload = new
            {
                key_prefix = keyPrefix,
                url_template = urlTemplate.Replace(" ", "%20")
            };

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            await githubApi.AddAutoLink(org, repo, keyPrefix, urlTemplate);

            // Assert
            githubClientMock.Verify(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload.ToJson())));
        }

        [Fact]
        public async Task GetAutoLinks_Calls_The_Right_Endpoint()
        {
            // Arrange
            const string org = "ORG";
            const string repo = "REPO";

            var url = $"https://api.github.com/repos/{org}/{repo}/autolinks";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock.Setup(x => x.GetAllAsync(It.IsAny<string>())).Returns(AsyncEnumerable.Empty<JToken>());
            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            await githubApi.GetAutoLinks(org, repo);

            // Assert
            githubClientMock.Verify(m => m.GetAllAsync(url));
        }

        [Fact]
        public async Task DeleteAutoLink_Calls_The_Right_Endpoint()
        {
            // Arrange
            const string org = "ORG";
            const string repo = "REPO";
            const int autoLinkId = 1;

            var url = $"https://api.github.com/repos/{org}/{repo}/autolinks/{autoLinkId}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            await githubApi.DeleteAutoLink(org, repo, autoLinkId);

            // Assert
            githubClientMock.Verify(m => m.DeleteAsync(url));
        }

        [Fact]
        public async Task CreateTeam_Returns_Created_Team_Id()
        {
            // Arrange
            const string org = "ORG";
            const string teamName = "TEAM_NAME";

            var url = $"https://api.github.com/orgs/{org}/teams";
            var payload = new { name = teamName, privacy = "closed" };

            const string teamId = "TEAM_ID";
            var response = $"{{\"id\": \"{teamId}\"}}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload.ToJson())))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = await githubApi.CreateTeam(org, teamName);

            // Assert
            result.Should().Be(teamId);
        }

        [Fact]
        public async Task GetTeams_Returns_All_Teams()
        {
            // Arrange
            const string org = "ORG";

            var url = $"https://api.github.com/orgs/{org}/teams";

            const string team1 = "TEAM_1";
            const string team2 = "TEAM_2";
            const string team3 = "TEAM_3";
            const string team4 = "TEAM_4";

            var teamsResult = new[]
            {
                new { id = 1, name = team1 },
                new { id = 2, name = team2 },
                new { id = 3, name = team3 },
                new { id = 4, name = team4 }
            }.ToAsyncJTokenEnumerable();

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.GetAllAsync(url))
                .Returns(teamsResult);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = (await githubApi.GetTeams(org)).ToArray();

            // Assert
            result.Should().HaveCount(4);
            result.Should().Equal(team1, team2, team3, team4);
        }

        [Fact]
        public async Task GetTeamMembers_Returns_Team_Members()
        {
            // Arrange
            const string org = "ORG";
            const string teamName = "TEAM_NAME";

            var url = $"https://api.github.com/orgs/{org}/teams/{teamName}/members?per_page=100";

            const string teamMember1 = "TEAM_MEMBER_1";
            const string teamMember2 = "TEAM_MEMBER_2";
            var responsePage1 = $@"
            [
                {{
                    ""login"": ""{teamMember1}"",
                    ""id"": 1
                }},
                {{
                    ""login"": ""{teamMember2}"", 
                    ""id"": 2
                }}
            ]";

            const string teamMember3 = "TEAM_MEMBER_3";
            const string teamMember4 = "TEAM_MEMBER_4";
            var responsePage2 = $@"
            [
                {{
                    ""login"": ""{teamMember3}"",
                    ""id"": 3
                }},
                {{
                    ""login"": ""{teamMember4}"", 
                    ""id"": 4
                }}
            ]";

            async IAsyncEnumerable<JToken> GetAllPages()
            {
                var jArrayPage1 = JArray.Parse(responsePage1);
                yield return jArrayPage1[0];
                yield return jArrayPage1[1];

                var jArrayPage2 = JArray.Parse(responsePage2);
                yield return jArrayPage2[0];
                yield return jArrayPage2[1];

                await Task.CompletedTask;
            }

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.GetAllAsync(url))
                .Returns(GetAllPages);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = (await githubApi.GetTeamMembers(org, teamName)).ToArray();

            // Assert
            result.Should().HaveCount(4);
            result.Should().Equal(teamMember1, teamMember2, teamMember3, teamMember4);
        }

        [Fact]
        public async Task GetTeamMembers_Retries_On_404()
        {
            // Arrange
            const string org = "ORG";
            const string teamName = "TEAM_NAME";

            var url = $"https://api.github.com/orgs/{org}/teams/{teamName}/members?per_page=100";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .SetupSequence(m => m.GetAllAsync(url))
                .Throws(new HttpRequestException(null, null, statusCode: HttpStatusCode.NotFound))
                .Throws(new HttpRequestException(null, null, statusCode: HttpStatusCode.NotFound))
                .Returns(new[]
                {
                    new { login = "Sally", id = 1 }
                }.ToAsyncJTokenEnumerable());

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = (await githubApi.GetTeamMembers(org, teamName)).ToArray();

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetRepos_Returns_Names_Of_All_Repositories()
        {
            // Arrange
            const string org = "ORG";
            var url = $"https://api.github.com/orgs/{org}/repos?per_page=100";

            const string repoName1 = "FOO";
            const string repoName2 = "BAR";
            var responsePage1 = $@"
            [
                {{
                    ""id"": 1,
                    ""name"": ""{repoName1}""
                }},
                {{
                    ""id"": 2,
                    ""name"": ""{repoName2}""
                }}
            ]";

            const string repoName3 = "BAZ";
            const string repoName4 = "QUX";
            var responsePage2 = $@"
            [
                {{
                    ""id"": 3,
                    ""name"": ""{repoName3}""
                }},
                {{
                    ""id"": 4,
                    ""name"": ""{repoName4}""
                }}
            ]";

            async IAsyncEnumerable<JToken> GetAllPages()
            {
                var jArrayPage1 = JArray.Parse(responsePage1);
                yield return jArrayPage1[0];
                yield return jArrayPage1[1];

                var jArrayPage2 = JArray.Parse(responsePage2);
                yield return jArrayPage2[0];
                yield return jArrayPage2[1];

                await Task.CompletedTask;
            }

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.GetAllAsync(url))
                .Returns(GetAllPages);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = (await githubApi.GetRepos(org)).ToArray();

            // Assert
            result.Should().HaveCount(4);
            result.Should().Equal(repoName1, repoName2, repoName3, repoName4);
        }

        [Fact]
        public async Task RemoveTeamMember_Calls_The_Right_Endpoint()
        {
            // Arrange
            const string org = "ORG";
            const string teamName = "TEAM_NAME";
            const string member = "MEMBER";

            var url = $"https://api.github.com/orgs/{org}/teams/{teamName}/memberships/{member}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock.Setup(m => m.DeleteAsync(url));

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            await githubApi.RemoveTeamMember(org, teamName, member);

            // Assert
            githubClientMock.Verify(m => m.DeleteAsync(url));
        }

        [Fact]
        public async Task AddTeamSync_Calls_The_Right_Endpoint_With_Payload()
        {
            // Arrange
            const string org = "ORG";
            const string teamName = "TEAM_NAME";
            const string groupId = "GROUP_ID";
            const string groupName = "GROUP_NAME";
            const string groupDesc = "GROUP_DESC";

            var url = $"https://api.github.com/orgs/{org}/teams/{teamName}/team-sync/group-mappings";
            var payload = new
            {
                groups = new[] { new { group_id = groupId, group_name = groupName, group_description = groupDesc } }
            };

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            await githubApi.AddTeamSync(org, teamName, groupId, groupName, groupDesc);

            // Assert
            githubClientMock.Verify(m => m.PatchAsync(url, It.Is<object>(x => x.ToJson() == payload.ToJson())));
        }

        [Fact]
        public async Task AddTeamToRepo_Calls_The_Right_Endpoint_With_Payload()
        {
            // Arrange
            const string org = "ORG";
            const string repo = "REPO";
            const string teamName = "TEAM_NAME";
            const string role = "ROLE";

            var url = $"https://api.github.com/orgs/{org}/teams/{teamName}/repos/{org}/{repo}";
            var payload = new { permission = role };

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            await githubApi.AddTeamToRepo(org, repo, teamName, role);

            // Assert
            githubClientMock.Verify(m => m.PutAsync(url, It.Is<object>(x => x.ToJson() == payload.ToJson())));
        }

        [Fact]
        public async Task GetOrganizationId_Returns_The_Org_Id()
        {
            // Arrange
            const string org = "ORG";
            const string orgId = "ORG_ID";

            var url = $"https://api.github.com/graphql";
            var payload =
                $"{{\"query\":\"query($login: String!) {{organization(login: $login) {{ login, id, name }} }}\",\"variables\":{{\"login\":\"{org}\"}}}}";
            var response = $@"
            {{
                ""data"": 
                    {{
                        ""organization"": 
                            {{
                                ""login"": ""{org}"",
                                ""id"": ""{orgId}"",
                                ""name"": ""github"" 
                            }} 
                    }} 
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = await githubApi.GetOrganizationId(org);

            // Assert
            result.Should().Be(orgId);
        }

        [Fact]
        public async Task CreateAdoMigrationSource_Returns_New_Migration_Source_Id()
        {
            // Arrange
            const string url = "https://api.github.com/graphql";
            const string orgId = "ORG_ID";
            var payload =
                "{\"query\":\"mutation createMigrationSource($name: String!, $url: String!, $ownerId: ID!, $type: MigrationSourceType!) " +
                "{ createMigrationSource(input: {name: $name, url: $url, ownerId: $ownerId, type: $type}) { migrationSource { id, name, url, type } } }\"" +
                $",\"variables\":{{\"name\":\"Azure DevOps Source\",\"url\":\"https://dev.azure.com\",\"ownerId\":\"{orgId}\",\"type\":\"AZURE_DEVOPS\"}},\"operationName\":\"createMigrationSource\"}}";
            const string actualMigrationSourceId = "MS_kgC4NjFhOTVjOTc4ZTRhZjEwMDA5NjNhOTdm";
            var response = $@"
            {{
                ""data"": {{
                    ""createMigrationSource"": {{
                        ""migrationSource"": {{
                            ""id"": ""{actualMigrationSourceId}"",
                            ""name"": ""Azure Devops Source"",
                            ""url"": ""https://dev.azure.com"",
                            ""type"": ""AZURE_DEVOPS""
                        }}
                    }}
                }}
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var expectedMigrationSourceId = await githubApi.CreateAdoMigrationSource(orgId);

            // Assert
            expectedMigrationSourceId.Should().Be(actualMigrationSourceId);
        }

        [Fact]
        public async Task CreateGhecMigrationSource_Returns_New_Migration_Source_Id()
        {
            // Arrange
            const string url = "https://api.github.com/graphql";
            const string orgId = "ORG_ID";
            var payload =
                "{\"query\":\"mutation createMigrationSource($name: String!, $url: String!, $ownerId: ID!, $type: MigrationSourceType!) " +
                "{ createMigrationSource(input: {name: $name, url: $url, ownerId: $ownerId, type: $type}) { migrationSource { id, name, url, type } } }\"" +
                $",\"variables\":{{\"name\":\"GHEC Source\",\"url\":\"https://github.com\",\"ownerId\":\"{orgId}\",\"type\":\"GITHUB_ARCHIVE\"}},\"operationName\":\"createMigrationSource\"}}";
            const string actualMigrationSourceId = "MS_kgC4NjFhOTVjOTc4ZTRhZjEwMDA5NjNhOTdm";
            var response = $@"
            {{
                ""data"": {{
                    ""createMigrationSource"": {{
                        ""migrationSource"": {{
                            ""id"": ""{actualMigrationSourceId}"",
                            ""name"": ""GHEC Source"",
                            ""url"": ""https://github.com"",
                            ""type"": ""GITHUB_ARCHIVE""
                        }}
                    }}
                }}
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var expectedMigrationSourceId = await githubApi.CreateGhecMigrationSource(orgId);

            // Assert
            expectedMigrationSourceId.Should().Be(actualMigrationSourceId);
        }

        [Fact]
        public async Task StartMigration_Returns_New_Repository_Migration_Id()
        {
            // Arrange
            const string migrationSourceId = "MIGRATION_SOURCE_ID";
            const string adoRepoUrl = "ADO_REPO_URL";
            const string orgId = "ORG_ID";
            const string repo = "REPO";
            const string url = "https://api.github.com/graphql";
            const string gitArchiveUrl = "GIT_ARCHIVE_URL";
            const string metadataArchiveUrl = "METADATA_ARCHIVE_URL";
            const string sourceToken = "SOURCE_TOKEN";
            const string targetToken = "TARGET_TOKEN";

            const string query = @"
                mutation startRepositoryMigration(
                    $sourceId: ID!,
                    $ownerId: ID!,
                    $sourceRepositoryUrl: URI!,
                    $repositoryName: String!,
                    $continueOnError: Boolean!,
                    $gitArchiveUrl: String,
                    $metadataArchiveUrl: String,
                    $accessToken: String!,
                    $githubPat: String,
                    $skipReleases: Boolean)";
            const string gql = @"
                startRepositoryMigration(
                    input: { 
                        sourceId: $sourceId,
                        ownerId: $ownerId,
                        sourceRepositoryUrl: $sourceRepositoryUrl,
                        repositoryName: $repositoryName,
                        continueOnError: $continueOnError,
                        gitArchiveUrl: $gitArchiveUrl,
                        metadataArchiveUrl: $metadataArchiveUrl,
                        accessToken: $accessToken,
                        githubPat: $githubPat,
                        skipReleases: $skipReleases
                    }
                ) {
                    repositoryMigration {
                        id,
                        migrationSource {
                            id,
                            name,
                            type
                        },
                        sourceUrl,
                        state,
                        failureReason
                    }
                  }";
            var payload = new
            {
                query = $"{query} {{ {gql} }}",
                variables = new
                {
                    sourceId = migrationSourceId,
                    ownerId = orgId,
                    sourceRepositoryUrl = adoRepoUrl,
                    repositoryName = repo,
                    continueOnError = true,
                    gitArchiveUrl,
                    metadataArchiveUrl,
                    accessToken = sourceToken,
                    githubPat = targetToken,
                    skipReleases = false
                },
                operationName = "startRepositoryMigration"
            };
            const string actualRepositoryMigrationId = "RM_kgC4NjFhNmE2NGU2ZWE1YTQwMDA5ODliZjhi";
            var response = $@"
            {{
                ""data"": {{
                    ""startRepositoryMigration"": {{
                        ""repositoryMigration"": {{
                            ""id"": ""{actualRepositoryMigrationId}"",
                            ""migrationSource"": {{
                                ""id"": ""MS_kgC4NjFhNmE2NDViNWZmOTEwMDA5MTZiMGQw"",
                                ""name"": ""Azure Devops Source"",
                                ""type"": ""AZURE_DEVOPS""
                            }},
                        ""sourceUrl"": ""https://dev.azure.com/github-inside-msft/Team-Demos/_git/Tiny"",
                        ""state"": ""QUEUED"",
                        ""failureReason"": """"
                        }}
                    }}
                }}
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload.ToJson())))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var expectedRepositoryMigrationId = await githubApi.StartMigration(migrationSourceId, adoRepoUrl, orgId, repo, sourceToken, targetToken, gitArchiveUrl, metadataArchiveUrl);

            // Assert
            expectedRepositoryMigrationId.Should().Be(actualRepositoryMigrationId);
        }

        [Fact]
        public async Task GetMigrationState_Returns_The_Migration_State()
        {
            // Arrange
            const string migrationId = "MIGRATION_ID";
            const string url = "https://api.github.com/graphql";

            var payload =
                "{\"query\":\"query($id: ID!) { node(id: $id) { ... on Migration { id, sourceUrl, migrationSource { name }, state, failureReason } } }\"" +
                $",\"variables\":{{\"id\":\"{migrationId}\"}}}}";
            const string actualMigrationState = "SUCCEEDED";
            var response = $@"
            {{
                ""data"": {{
                    ""node"": {{
                        ""id"": ""RM_kgC4NjFhNmE2ZWY1NmE4MjAwMDA4NjA5NTZi"",
                        ""sourceUrl"": ""https://github.com/import-testing/archive-export-testing"",
                        ""migrationSource"": {{
                            ""name"": ""GHEC Archive Source""
                        }},
                        ""state"": ""{actualMigrationState}"",
                        ""failureReason"": """"
                    }}
                }}
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var expectedMigrationState = await githubApi.GetMigrationState(migrationId);

            // Assert
            expectedMigrationState.Should().Be(actualMigrationState);
        }

        [Fact]
        public async Task GetMigrationState_Retries_On_502()
        {
            // Arrange
            const string migrationId = "MIGRATION_ID";
            const string url = "https://api.github.com/graphql";

            var payload =
                "{\"query\":\"query($id: ID!) { node(id: $id) { ... on Migration { id, sourceUrl, migrationSource { name }, state, failureReason } } }\"" +
                $",\"variables\":{{\"id\":\"{migrationId}\"}}}}";
            const string actualMigrationState = "SUCCEEDED";
            var response = $@"
            {{
                ""data"": {{
                    ""node"": {{
                        ""id"": ""RM_kgC4NjFhNmE2ZWY1NmE4MjAwMDA4NjA5NTZi"",
                        ""sourceUrl"": ""https://github.com/import-testing/archive-export-testing"",
                        ""migrationSource"": {{
                            ""name"": ""GHEC Archive Source""
                        }},
                        ""state"": ""{actualMigrationState}"",
                        ""failureReason"": """"
                    }}
                }}
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .SetupSequence(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .Throws(new HttpRequestException(null, null, statusCode: HttpStatusCode.BadGateway))
                .Throws(new HttpRequestException(null, null, statusCode: HttpStatusCode.BadGateway))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var expectedMigrationState = await githubApi.GetMigrationState(migrationId);

            // Assert
            expectedMigrationState.Should().Be(actualMigrationState);
        }

        [Fact]
        public async Task GetMigrationFailureReason_Returns_The_Migration_Failure_Reason()
        {
            // Arrange
            const string migrationId = "MIGRATION_ID";
            const string url = "https://api.github.com/graphql";

            var payload =
                "{\"query\":\"query($id: ID!) { node(id: $id) { ... on Migration { id, sourceUrl, migrationSource { name }, state, failureReason } } }\"" +
                $",\"variables\":{{\"id\":\"{migrationId}\"}}}}";
            const string actualFailureReason = "FAILURE_REASON";
            var response = $@"
            {{
                ""data"": {{
                    ""node"": {{
                        ""id"": ""RM_kgC4NjFhNmE2ZWY1NmE4MjAwMDA4NjA5NTZi"",
                        ""sourceUrl"": ""https://github.com/import-testing/archive-export-testing"",
                        ""migrationSource"": {{
                            ""name"": ""GHEC Archive Source""
                        }},
                        ""state"": ""FAILED"",
                        ""failureReason"": ""{actualFailureReason}""
                    }}
                }}
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var expectedFailureReason = await githubApi.GetMigrationFailureReason(migrationId);

            // Assert
            expectedFailureReason.Should().Be(actualFailureReason);
        }

        [Fact]
        public async Task GetMigrationFailureReason_Retries_On_502()
        {
            // Arrange
            const string migrationId = "MIGRATION_ID";
            const string url = "https://api.github.com/graphql";

            var payload =
                "{\"query\":\"query($id: ID!) { node(id: $id) { ... on Migration { id, sourceUrl, migrationSource { name }, state, failureReason } } }\"" +
                $",\"variables\":{{\"id\":\"{migrationId}\"}}}}";
            const string actualFailureReason = "FAILURE_REASON";
            var response = $@"
            {{
                ""data"": {{
                    ""node"": {{
                        ""id"": ""RM_kgC4NjFhNmE2ZWY1NmE4MjAwMDA4NjA5NTZi"",
                        ""sourceUrl"": ""https://github.com/import-testing/archive-export-testing"",
                        ""migrationSource"": {{
                            ""name"": ""GHEC Archive Source""
                        }},
                        ""state"": ""FAILED"",
                        ""failureReason"": ""{actualFailureReason}""
                    }}
                }}
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .SetupSequence(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .Throws(new HttpRequestException(null, null, statusCode: HttpStatusCode.BadGateway))
                .Throws(new HttpRequestException(null, null, statusCode: HttpStatusCode.BadGateway))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var expectedFailureReason = await githubApi.GetMigrationFailureReason(migrationId);

            // Assert
            expectedFailureReason.Should().Be(actualFailureReason);
        }

        [Fact]
        public async Task GetIdpGroupId_Returns_The_Idp_Group_Id()
        {
            // Arrange
            const string org = "ORG";
            const string groupName = "GROUP_NAME";

            var url = $"https://api.github.com/orgs/{org}/external-groups";
            const int expectedGroupId = 123;
            var response = $@"
            {{
                ""groups"": [
                    {{
                       ""group_id"": ""{expectedGroupId}"",
                       ""group_name"": ""{groupName}"",
                       ""updated_at"": ""2021-01-24T11:31:04-06:00""
                    }},
                    {{
                       ""group_id"": ""456"",
                       ""group_name"": ""Octocat admins"",
                       ""updated_at"": ""2021-03-24T11:31:04-06:00""
                    }},
                ]
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.GetAsync(url))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var actualGroupId = await githubApi.GetIdpGroupId(org, groupName);

            // Assert
            actualGroupId.Should().Be(expectedGroupId);
        }

        [Fact]
        public async Task GetTeamSlug_Returns_The_Team_Slug()
        {
            // Arrange
            const string org = "ORG";
            const string teamName = "TEAM_NAME";

            var url = $"https://api.github.com/orgs/{org}/teams";
            const string expectedTeamSlug = "justice-league";

            var response = new[]
            {
                new
                {
                    id = 1,
                    node_id = "MDQ6VGVhbTE=",
                    url = "https://api.github.com/teams/1",
                    html_url = "https://github.com/orgs/github/teams/justice-league",
                    name = teamName,
                    slug = expectedTeamSlug,
                    description = "A great team.",
                    privacy = "closed",
                    permission = "admin",
                    members_url = "https://api.github.com/teams/1/members/membber",
                    repositories_url = "https://api.github.com/teams/1/repos",
                }
            }.ToAsyncJTokenEnumerable();

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.GetAllAsync(url))
                .Returns(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var actualTeamSlug = await githubApi.GetTeamSlug(org, teamName);

            // Assert
            actualTeamSlug.Should().Be(expectedTeamSlug);
        }

        [Fact]
        public async Task AddEmuGroupToTeam_Calls_The_Right_Endpoint_With_Payload()
        {
            // Arrange
            const string org = "ORG";
            const string teamSlug = "TEAM_SLUG";
            const int groupId = 1;

            var url = $"https://api.github.com/orgs/{org}/teams/{teamSlug}/external-groups";
            var payload = new { group_id = groupId };

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            await githubApi.AddEmuGroupToTeam(org, teamSlug, groupId);

            // Assert
            githubClientMock.Verify(m => m.PatchAsync(url, It.Is<object>(x => x.ToJson() == payload.ToJson())));
        }

        [Fact]
        public async Task GrantMigratorRole_Returns_True_On_Success()
        {
            // Arrange
            const string org = "ORG";
            const string actor = "ACTOR";
            const string actorType = "ACTOR_TYPE";
            const string url = "https://api.github.com/graphql";

            var payload =
                "{\"query\":\"mutation grantMigratorRole ( $organizationId: ID!, $actor: String!, $actor_type: ActorType! ) " +
                "{ grantMigratorRole( input: {organizationId: $organizationId, actor: $actor, actorType: $actor_type }) { success } }\"" +
                $",\"variables\":{{\"organizationId\":\"{org}\",\"actor\":\"{actor}\",\"actor_type\":\"{actorType}\"}}," +
                "\"operationName\":\"grantMigratorRole\"}";
            const bool expectedSuccessState = true;
            var response = $@"
            {{
                ""data"": {{
                    ""grantMigratorRole"": {{
                        ""success"": {expectedSuccessState.ToString().ToLower()}
                    }}
                }}
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var actualSuccessState = await githubApi.GrantMigratorRole(org, actor, actorType);

            // Assert
            actualSuccessState.Should().BeTrue();
        }

        [Fact]
        public async Task GrantMigratorRole_Returns_False_On_HttpRequestException()
        {
            // Arrange
            const string org = "ORG";
            const string actor = "ACTOR";
            const string actorType = "ACTOR_TYPE";
            const string url = "https://api.github.com/graphql";

            var payload =
                "{\"query\":\"mutation grantMigratorRole ( $organizationId: ID!, $actor: String!, $actor_type: ActorType! ) " +
                "{ grantMigratorRole( input: {organizationId: $organizationId, actor: $actor, actorType: $actor_type }) { success } }\"" +
                $",\"variables\":{{\"organizationId\":\"{org}\",\"actor\":\"{actor}\",\"actor_type\":\"{actorType}\"}}," +
                "\"operationName\":\"grantMigratorRole\"}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .Throws<HttpRequestException>();

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var actualSuccessState = await githubApi.GrantMigratorRole(org, actor, actorType);

            // Assert
            actualSuccessState.Should().BeFalse();
        }

        [Fact]
        public async Task RevokeMigratorRole_Returns_True_On_Success()
        {
            // Arrange
            const string org = "ORG";
            const string actor = "ACTOR";
            const string actorType = "ACTOR_TYPE";
            const string url = "https://api.github.com/graphql";

            var payload =
                "{\"query\":\"mutation revokeMigratorRole ( $organizationId: ID!, $actor: String!, $actor_type: ActorType! ) " +
                "{ revokeMigratorRole( input: {organizationId: $organizationId, actor: $actor, actorType: $actor_type }) { success } }\"" +
                $",\"variables\":{{\"organizationId\":\"{org}\",\"actor\":\"{actor}\",\"actor_type\":\"{actorType}\"}}," +
                "\"operationName\":\"revokeMigratorRole\"}";
            const bool expectedSuccessState = true;
            var response = $@"
            {{
                ""data"": {{
                    ""revokeMigratorRole"": {{
                        ""success"": {expectedSuccessState.ToString().ToLower()}
                    }}
                }}
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var actualSuccessState = await githubApi.RevokeMigratorRole(org, actor, actorType);

            // Assert
            actualSuccessState.Should().BeTrue();
        }

        [Fact]
        public async Task RevokeMigratorRole_Returns_False_On_HttpRequestException()
        {
            // Arrange
            const string org = "ORG";
            const string actor = "ACTOR";
            const string actorType = "ACTOR_TYPE";
            const string url = "https://api.github.com/graphql";

            var payload =
                "{\"query\":\"mutation revokeMigratorRole ( $organizationId: ID!, $actor: String!, $actor_type: ActorType! ) " +
                "{ revokeMigratorRole( input: {organizationId: $organizationId, actor: $actor, actorType: $actor_type }) { success } }\"" +
                $",\"variables\":{{\"organizationId\":\"{org}\",\"actor\":\"{actor}\",\"actor_type\":\"{actorType}\"}}," +
                "\"operationName\":\"revokeMigratorRole\"}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .Throws<HttpRequestException>();

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var actualSuccessState = await githubApi.RevokeMigratorRole(org, actor, actorType);

            // Assert
            actualSuccessState.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteRepo_Calls_The_Right_Endpoint()
        {
            // Arrange
            const string org = "FOO-ORG";
            const string repo = "FOO-REPO";

            var url = $"https://api.github.com/repos/{org}/{repo}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            await githubApi.DeleteRepo(org, repo);

            // Assert
            githubClientMock.Verify(m => m.DeleteAsync(url));
        }

        [Fact]
        public async Task GetMigrationStates_Returns_All_Migration_States_For_An_Org()
        {
            // Arrange
            const string url = "https://api.github.com/graphql";
            const string orgId = "ORG_ID";

            var payload =
                @"{""query"":""query($id: ID!, $first: Int, $after: String) { 
                                    node(id: $id) { 
                                        ... on Organization { 
                                            login, 
                                            repositoryMigrations(first: $first, after: $after) {
                                                pageInfo {
                                                    endCursor
                                                    hasNextPage
                                                }
                                                totalCount
                                                nodes {
                                                    id
                                                    sourceUrl
                                                    migrationSource { name }
                                                    state
                                                    failureReason
                                                    createdAt
                                                }
                                            }
                                        }
                                    } 
                                }""" +
                $",\"variables\":{{\"id\":\"{orgId}\"}}}}";

            var migration1 = new
            {
                id = Guid.NewGuid().ToString(),
                sourceUrl = "https://dev.azure.com/org/team_project/_git/repo_1",
                migrationSource = new { name = "Azure Devops Source" },
                state = RepositoryMigrationStatus.Succeeded,
                failureReason = "",
                createdAt = DateTime.UtcNow
            };
            var migration2 = new
            {
                id = Guid.NewGuid().ToString(),
                sourceUrl = "https://dev.azure.com/org/team_project/_git/repo_2",
                migrationSource = new { name = "Azure Devops Source" },
                state = RepositoryMigrationStatus.InProgress,
                failureReason = "",
                createdAt = DateTime.UtcNow
            };
            var migration3 = new
            {
                id = Guid.NewGuid().ToString(),
                sourceUrl = "https://dev.azure.com/org/team_project/_git/repo_3",
                migrationSource = new { name = "Azure Devops Source" },
                state = RepositoryMigrationStatus.Failed,
                failureReason = "FAILURE_REASON",
                createdAt = DateTime.UtcNow
            };
            var migration4 = new
            {
                id = Guid.NewGuid().ToString(),
                sourceUrl = "https://dev.azure.com/org/team_project/_git/repo_4",
                migrationSource = new { name = "Azure Devops Source" },
                state = RepositoryMigrationStatus.Queued,
                failureReason = "",
                createdAt = DateTime.UtcNow
            };

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostGraphQLWithPaginationAsync(
                    url,
                    It.Is<object>(x => Compact(x.ToJson()) == Compact(payload)),
                    It.IsAny<Func<JObject, JArray>>(),
                    It.IsAny<Func<JObject, JObject>>(),
                    It.IsAny<int>(),
                    null))
                .Returns(new[]
                {
                    JToken.FromObject(migration1),
                    JToken.FromObject(migration2),
                    JToken.FromObject(migration3),
                    JToken.FromObject(migration4)
                }.ToAsyncEnumerable());

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var migrationStates = (await githubApi.GetMigrationStates(orgId)).ToArray();

            // Assert
            migrationStates.Should().HaveCount(4);
            migrationStates.Should().Contain(new[]
            {
                (Migration: migration1.id, State: migration1.state),
                (Migration: migration2.id, State: migration2.state),
                (Migration: migration3.id, State: migration3.state),
                (Migration: migration4.id, State: migration4.state)
            });
        }

        [Fact]
        public async Task GetUserId_Returns_The_User_Id()
        {
            // Arrange
            const string login = "mona";
            const string userId = "NDQ5VXNlcjc4NDc5MzU=";

            var url = $"https://api.github.com/graphql";
            var payload =
                $"{{\"query\":\"query($login: String!) {{user(login: $login) {{ id, name }} }}\",\"variables\":{{\"login\":\"{login}\"}}}}";

            var response = $@"
            {{
                ""data"": 
                    {{
                        ""user"": 
                            {{
                                ""id"": ""{userId}"",
                                ""name"": ""{login}"" 
                            }} 
                    }} 
            }}";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = await githubApi.GetUserId(login);

            // Assert
            result.Should().Be(userId);
        }

        [Fact]
        public async Task GetUserId_For_No_Existant_User_Returns_Null()
        {
            // Arrange
            const string login = "idonotexist";

            var url = $"https://api.github.com/graphql";
            var payload =
                $"{{\"query\":\"query($login: String!) {{user(login: $login) {{ id, name }} }}\",\"variables\":{{\"login\":\"{login}\"}}}}";

            var response = @"{
	            ""data"": {
                    ""user"": null
                },
	            ""errors"": [
		            {
			            ""type"": ""NOT_FOUND"",
			            ""path"": [
				            ""user""
			            ],
			            ""locations"": [
				            {
					            ""line"": 4,
					            ""column"": 3
                            }
			            ],
			            ""message"": ""Could not resolve to a User with the login of 'idonotexist'.""
		            }
	            ]
            }";

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostAsync(url, It.Is<object>(x => x.ToJson() == payload)))
                .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = await githubApi.GetUserId(login);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetMannequin_WithNoUser_Returns_Empty()
        {
            // Arrange
            const string orgId = "ORG_ID";
            const string login = "monadoessnotexist";

            var url = $"https://api.github.com/graphql";

            var payload = @"{""query"":""query($id: ID!, $first: Int, $after: String) { 
                node(id: $id) {
                    ... on Organization {
                        mannequins(first: $first, after: $after) {
                            pageInfo {
                                endCursor
                                hasNextPage
                            }
                            nodes {
                                login
                                id
                                claimant {
                                    login
                                    id
                                }
                            }
                        }
                    }
                }
            }""" +
            $",\"variables\":{{\"id\":\"{orgId}\"}}}}";

            var mannequin = new
            {
                login = "mona",
                id = "DUMMYID",
                claimant = new { }
            };


            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostGraphQLWithPaginationAsync(
                    url,
                    It.Is<object>(x => Compact(x.ToJson()) == Compact(payload)),
                    It.IsAny<Func<JObject, JArray>>(),
                    It.IsAny<Func<JObject, JObject>>(),
                    It.IsAny<int>(),
                    null))
                 .Returns(new[] { JToken.FromObject(mannequin) }.ToAsyncEnumerable());


            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = await githubApi.GetMannequin(orgId, login);

            // Assert
            result.Id.Should().BeNull();
        }

        [Fact]
        public async Task GetMannequin_Returns_Mannequin()
        {
            // Arrange
            const string orgId = "ORG_ID";
            const string login = "mona";

            var url = $"https://api.github.com/graphql";

            var payload =
    @"{""query"":""query($id: ID!, $first: Int, $after: String) { 
                node(id: $id) {
                    ... on Organization {
                        mannequins(first: $first, after: $after) {
                            pageInfo {
                                endCursor
                                hasNextPage
                            }
                            nodes {
                                login
                                id
                                claimant {
                                    login
                                    id
                                }
                            }
                        }
                    }
                }
            }""" +
    $",\"variables\":{{\"id\":\"{orgId}\"}}}}";

            var mannequin = new
            {
                login = "mona",
                id = "DUMMYID",
                claimant = new { }
            };


            var githubClientMock = TestHelpers.CreateMock<GithubClient>();
            githubClientMock
                .Setup(m => m.PostGraphQLWithPaginationAsync(
                    url,
                    It.Is<object>(x => Compact(x.ToJson()) == Compact(payload)),
                    It.IsAny<Func<JObject, JArray>>(),
                    It.IsAny<Func<JObject, JObject>>(),
                    It.IsAny<int>(),
                    null))
                    .Returns(new[] { JToken.FromObject(mannequin) }.ToAsyncEnumerable());

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = await githubApi.GetMannequin(orgId, login.ToUpperInvariant()); // ensure case insensitivity

            // Assert
            result.Id.Should().Be("DUMMYID");
            result.Login.Should().Be(login);
            result.MappedUser.Should().BeNull();
        }

        [Fact]
        public async Task ReclaimMannequin_Returns_Error()
        {
            // Arrange
            const string orgId = "dummyorgid";
            const string mannequinId = "NDQ5VXNlcjc4NDc5MzU=";
            const string targetUserId = "ND5TVXNlcjc4NDc5MzU=";

            var url = $"https://api.github.com/graphql";

            var payload = @"{""query"":""mutation($orgId: ID!,$sourceId: ID!,$targetId: ID!) { createAttributionInvitation(
		            input: { ownerId: $orgId, sourceId: $sourceId, targetId: $targetId }
	            ) {
		            source {
			            ... on Mannequin {
				            id
				            login
			            }
		            }

		            target {
			            ... on User {
				            id
				            login
			            }
		            }
                }
            }""" + $",\"variables\":{{\"orgId\":\"{orgId}\", \"sourceId\":\"{mannequinId}\", \"targetId\":\"{targetUserId}\"}}}}";

            var response = $@"{{
                ""data"": {{
                                ""createAttributionInvitation"": null
                    }},
                ""errors"": [{{
                                ""type"": ""UNPROCESSABLE"",
                    ""path"": [""createAttributionInvitation""],
                    ""locations"": [{{
                                        ""line"": 2,
                        ""column"": 14
                    }}],
                    ""message"": ""Target must be a member of the octocat organization""
                }}]
            }}";

            var expectedReclaimMannequinResponse = new MannequinReclaimResult()
            {
                Data = new CreateAttributionInvitationData()
                {
                    CreateAttributionInvitation = null
                },
                Errors = new Collection<ErrorData>{new ErrorData
                {
                    Type = "UNPROCESSABLE",
                    Message = "Target must be a member of the octocat organization",
                    Path = new Collection<string> { "createAttributionInvitation" },
                    Locations = new Collection<Location> {
                                new Location()
                                {
                                    Line = 2,
                                    Column = 14
                                }
                            }
                    }
                }
            };

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();

            githubClientMock
                .Setup(m => m.PostAsync(url,
                It.Is<object>(x => Compact(x.ToJson()) == Compact(payload))))
                    .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = await githubApi.ReclaimMannequin(orgId, mannequinId, targetUserId);

            // Assert
            result.Should().BeEquivalentTo(expectedReclaimMannequinResponse);
        }

        [Fact]
        public async Task ReclaimMannequin_Returns_Success()
        {
            // Arrange
            const string orgId = "dummyorgid";
            const string mannequinId = "NDQ5VXNlcjc4NDc5MzU=";
            const string mannequinUser = "mona";
            const string targetUserId = "ND5TVXNlcjc4NDc5MzU=";
            const string targetUser = "lisa";

            var url = $"https://api.github.com/graphql";

            var payload = @"{""query"":""mutation($orgId: ID!,$sourceId: ID!,$targetId: ID!) { createAttributionInvitation(
		            input: { ownerId: $orgId, sourceId: $sourceId, targetId: $targetId }
	            ) {
		            source {
			            ... on Mannequin {
				            id
				            login
			            }
		            }

		            target {
			            ... on User {
				            id
				            login
			            }
		            }
                }
            }""" + $",\"variables\":{{\"orgId\":\"{orgId}\", \"sourceId\":\"{mannequinId}\", \"targetId\":\"{targetUserId}\"}}}}";

            var response = $@"{{
                ""data"": {{
                    ""createAttributionInvitation"": {{
                        ""source"": {{
                            ""id"": ""{mannequinId}"",
                            ""login"": ""{mannequinUser}""
                        }},
                        ""target"": {{
                            ""id"": ""{targetUserId}"",
                            ""login"": ""{targetUser}""
                        }}
                    }}
                }}
            }}";

            var expectedReclaimMannequinResponse = new MannequinReclaimResult()
            {
                Data = new CreateAttributionInvitationData()
                {
                    CreateAttributionInvitation = new CreateAttributionInvitation()
                    {
                        Source = new UserInfo()
                        {
                            Id = mannequinId,
                            Login = mannequinUser
                        },
                        Target = new UserInfo()
                        {
                            Id = targetUserId,
                            Login = targetUser
                        }
                    }
                }
            };

            var githubClientMock = TestHelpers.CreateMock<GithubClient>();

            githubClientMock
                .Setup(m => m.PostAsync(url,
                It.Is<object>(x => Compact(x.ToJson()) == Compact(payload))))
                    .ReturnsAsync(response);

            // Act
            var githubApi = new GithubApi(githubClientMock.Object, Api_Url, _retryPolicy);
            var result = await githubApi.ReclaimMannequin(orgId, mannequinId, targetUserId);

            // Assert
            result.Should().BeEquivalentTo(expectedReclaimMannequinResponse);
        }
        private string Compact(string source) =>
            source
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", "")
                .Replace("\\r", "")
                .Replace("\\n", "")
                .Replace("\\t", "")
                .Replace(" ", "");
    }
}
