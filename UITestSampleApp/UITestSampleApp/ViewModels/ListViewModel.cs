﻿using System;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xamarin.Forms;

using Plugin.Connectivity;

namespace UITestSampleApp
{
	public class ListViewModel : BaseViewModel
	{
		#region Fields
		bool _isDataLoadingFromBackend;

		ICommand _PullToRefreshCommand;

		List<ListPageDataModel> _dataList;
		#endregion

		#region Constructors
		public ListViewModel()
		{
			Task.Run(async () => await RefreshDataAsync());
		}

		#endregion

		#region Events
		public event EventHandler LoadingDataFromBackendCompleted;
		#endregion

		#region Properties
		public List<ListPageDataModel> DataList
		{
			get { return _dataList; }
			set { SetProperty(ref _dataList, value); }
		}

		public bool IsDataLoading
		{
			get { return _isDataLoadingFromBackend; }
			set { SetProperty(ref _isDataLoadingFromBackend, value); }
		}

		public ICommand PullToRefreshCommand => _PullToRefreshCommand ??
			(_PullToRefreshCommand = new Command(async () => await ExecutePullToRefreshCommanded()));
		#endregion

		#region Methods
		async Task RefreshDataFromAzureAsync()
		{
			var isAzureDatabaseReachable = await CrossConnectivity.Current.IsRemoteReachable(AzureConstants.AzureDataServiceUrl, 80, 1000);
			if (!CrossConnectivity.Current.IsConnected || !isAzureDatabaseReachable)
				return;

			try
			{
				await DependencyService.Get<IDataService>().SyncItemsAsync<ListPageDataModel>();
				var dataListAsIEnumerable = await DependencyService.Get<IDataService>().GetItemsAsync<ListPageDataModel>();
				DataList = dataListAsIEnumerable.ToList();
			}
			catch (Exception e)
			{
				AnalyticsHelpers.Log("Error Retrieving Data From Azure", e.Message, e);
			}
		}

		async Task RefreshDataFromLocalDatabaseAsync()
		{
			try
			{
				var dataListAsIEnumerable = await DependencyService.Get<IDataService>().GetItemsAsync<ListPageDataModel>();
				DataList = dataListAsIEnumerable?.ToList();
			}
			catch (Exception e)
			{
				AnalyticsHelpers.Log("Error Retriving Data From Local Database", e.Message, e);
			}
		}

		async Task ExecutePullToRefreshCommanded()
		{
			AnalyticsHelpers.TrackEvent(AnalyticsConstants.PullToRefreshCommanded);

			await RefreshDataAsync();

			OnLoadingDataFromBackendCompleted();
		}

		async Task RefreshDataAsync()
		{
			IsDataLoading = true;

			await RefreshDataFromLocalDatabaseAsync();
			await RefreshDataFromAzureAsync();

			OnLoadingDataFromBackendCompleted();

			IsDataLoading = false;
		}

		void OnLoadingDataFromBackendCompleted() =>
			LoadingDataFromBackendCompleted?.Invoke(null, EventArgs.Empty);
		#endregion
	}
}
