// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.Limitations.Client.AutorestClient
{
    using Models;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for LykkelimitationsService.
    /// </summary>
    public static partial class LykkelimitationsServiceExtensions
    {
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            public static IsAliveResponse IsAlive(this ILykkelimitationsService operations)
            {
                return operations.IsAliveAsync().GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IsAliveResponse> IsAliveAsync(this ILykkelimitationsService operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.IsAliveWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='postModel'>
            /// </param>
            public static LimitationCheckResult ApiLimitationsPost(this ILykkelimitationsService operations, LimitCheckRequestModel postModel = default(LimitCheckRequestModel))
            {
                return operations.ApiLimitationsPostAsync(postModel).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='postModel'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<LimitationCheckResult> ApiLimitationsPostAsync(this ILykkelimitationsService operations, LimitCheckRequestModel postModel = default(LimitCheckRequestModel), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiLimitationsPostWithHttpMessagesAsync(postModel, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='period'>
            /// Possible values include: 'Day', 'Month'
            /// </param>
            /// <param name='clientId'>
            /// </param>
            public static ClientData ApiLimitationsGetClientDataPost(this ILykkelimitationsService operations, LimitationPeriod period, string clientId = default(string))
            {
                return operations.ApiLimitationsGetClientDataPostAsync(period, clientId).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='period'>
            /// Possible values include: 'Day', 'Month'
            /// </param>
            /// <param name='clientId'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<ClientData> ApiLimitationsGetClientDataPostAsync(this ILykkelimitationsService operations, LimitationPeriod period, string clientId = default(string), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiLimitationsGetClientDataPostWithHttpMessagesAsync(period, clientId, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientId'>
            /// </param>
            /// <param name='operationId'>
            /// </param>
            public static void ApiLimitationsRemoveClientOperationDelete(this ILykkelimitationsService operations, string clientId = default(string), string operationId = default(string))
            {
                operations.ApiLimitationsRemoveClientOperationDeleteAsync(clientId, operationId).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientId'>
            /// </param>
            /// <param name='operationId'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task ApiLimitationsRemoveClientOperationDeleteAsync(this ILykkelimitationsService operations, string clientId = default(string), string operationId = default(string), CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiLimitationsRemoveClientOperationDeleteWithHttpMessagesAsync(clientId, operationId, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientId'>
            /// </param>
            public static AccumulatedDepositsModel ApiLimitationsGetAccumulatedDepositsPost(this ILykkelimitationsService operations, string clientId = default(string))
            {
                return operations.ApiLimitationsGetAccumulatedDepositsPostAsync(clientId).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientId'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<AccumulatedDepositsModel> ApiLimitationsGetAccumulatedDepositsPostAsync(this ILykkelimitationsService operations, string clientId = default(string), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiLimitationsGetAccumulatedDepositsPostWithHttpMessagesAsync(clientId, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            public static IList<SwiftTransferLimitation> ApiSwiftLimitationsGet(this ILykkelimitationsService operations)
            {
                return operations.ApiSwiftLimitationsGetAsync().GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IList<SwiftTransferLimitation>> ApiSwiftLimitationsGetAsync(this ILykkelimitationsService operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiSwiftLimitationsGetWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='limitations'>
            /// </param>
            public static void ApiSwiftLimitationsPost(this ILykkelimitationsService operations, IList<SwiftTransferLimitation> limitations = default(IList<SwiftTransferLimitation>))
            {
                operations.ApiSwiftLimitationsPostAsync(limitations).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='limitations'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task ApiSwiftLimitationsPostAsync(this ILykkelimitationsService operations, IList<SwiftTransferLimitation> limitations = default(IList<SwiftTransferLimitation>), CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiSwiftLimitationsPostWithHttpMessagesAsync(limitations, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='asset'>
            /// </param>
            public static SwiftTransferLimitation ApiSwiftLimitationsByAssetGet(this ILykkelimitationsService operations, string asset)
            {
                return operations.ApiSwiftLimitationsByAssetGetAsync(asset).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='asset'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<SwiftTransferLimitation> ApiSwiftLimitationsByAssetGetAsync(this ILykkelimitationsService operations, string asset, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiSwiftLimitationsByAssetGetWithHttpMessagesAsync(asset, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='asset'>
            /// </param>
            public static void ApiSwiftLimitationsByAssetDelete(this ILykkelimitationsService operations, string asset)
            {
                operations.ApiSwiftLimitationsByAssetDeleteAsync(asset).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='asset'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task ApiSwiftLimitationsByAssetDeleteAsync(this ILykkelimitationsService operations, string asset, CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiSwiftLimitationsByAssetDeleteWithHttpMessagesAsync(asset, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientTier'>
            /// </param>
            public static void ApiTiersSetTierToClientPost(this ILykkelimitationsService operations, ClientTier clientTier = default(ClientTier))
            {
                operations.ApiTiersSetTierToClientPostAsync(clientTier).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientTier'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task ApiTiersSetTierToClientPostAsync(this ILykkelimitationsService operations, ClientTier clientTier = default(ClientTier), CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiTiersSetTierToClientPostWithHttpMessagesAsync(clientTier, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientId'>
            /// </param>
            public static Tier ApiTiersGetClientTierGet(this ILykkelimitationsService operations, string clientId = default(string))
            {
                return operations.ApiTiersGetClientTierGetAsync(clientId).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientId'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<Tier> ApiTiersGetClientTierGetAsync(this ILykkelimitationsService operations, string clientId = default(string), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiTiersGetClientTierGetWithHttpMessagesAsync(clientId, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientId'>
            /// </param>
            public static IList<IClientTierLogRecord> ApiTiersGetClientTierLogGet(this ILykkelimitationsService operations, string clientId = default(string))
            {
                return operations.ApiTiersGetClientTierLogGetAsync(clientId).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientId'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IList<IClientTierLogRecord>> ApiTiersGetClientTierLogGetAsync(this ILykkelimitationsService operations, string clientId = default(string), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiTiersGetClientTierLogGetWithHttpMessagesAsync(clientId, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tierModel'>
            /// </param>
            public static void ApiTiersSaveTierPost(this ILykkelimitationsService operations, TierRequestModel tierModel = default(TierRequestModel))
            {
                operations.ApiTiersSaveTierPostAsync(tierModel).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tierModel'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task ApiTiersSaveTierPostAsync(this ILykkelimitationsService operations, TierRequestModel tierModel = default(TierRequestModel), CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiTiersSaveTierPostWithHttpMessagesAsync(tierModel, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            public static IList<Tier> ApiTiersLoadTiersPost(this ILykkelimitationsService operations)
            {
                return operations.ApiTiersLoadTiersPostAsync().GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IList<Tier>> ApiTiersLoadTiersPostAsync(this ILykkelimitationsService operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiTiersLoadTiersPostWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            public static Tier ApiTiersLoadTierPost(this ILykkelimitationsService operations, string id = default(string))
            {
                return operations.ApiTiersLoadTierPostAsync(id).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<Tier> ApiTiersLoadTierPostAsync(this ILykkelimitationsService operations, string id = default(string), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiTiersLoadTierPostWithHttpMessagesAsync(id, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

    }
}
