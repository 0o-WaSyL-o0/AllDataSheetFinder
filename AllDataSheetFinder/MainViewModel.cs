﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Data;
using MVVMUtils;
using MVVMUtils.Collections;
using AllDataSheetFinder.Controls;
using Microsoft.Win32;

namespace AllDataSheetFinder
{
    public sealed class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            m_searchCommand = new RelayCommand(Search, CanSearch);
            m_openPdfCommand = new RelayCommand(OpenPdf);
            m_loadMoreResultCommand = new RelayCommand(LoadMoreResults, CanLoadMoreResults);
            m_addToFavouritesCommand = new RelayCommand(AddToFavourites);
            m_saveFavouritesCommand = new RelayCommand(SaveFavourites);
            m_showFavouritesCommand = new RelayCommand(ShowFavourites, CanShowFavourites);
            m_settingsCommand = new RelayCommand(Settings);
            m_requestMoreInfoCommand = new RelayCommand(RequestMoreInfo);
            m_addCustomCommand = new RelayCommand(AddCustom);

            m_filteredResults = CollectionViewSource.GetDefaultView(m_searchResults);
            m_filteredResults.Filter = (x) =>
            {
                if (!IsFavouritesMode) return true;

                PartViewModel part = (PartViewModel)x;
                string[] tokens = m_searchField.ToUpper().Split(' ');
                string upperName = (part.Name == null ? string.Empty : part.Name.ToUpper());

                foreach (var item in tokens)
                {
                    var result = part.Tags.FirstOrDefault(tag => tag.Value.ToUpper().StartsWith(item));
                    if (result == null)
                    {
                        if (!upperName.Contains(item)) return false;
                    }
                }

                return true;
            };
            m_filteredResults.Refresh();

            m_savedParts = new SynchronizedObservableCollection<PartViewModel, Part>(Global.SavedParts, (m) => new PartViewModel(m));
            RemoveUnavailableSavedParts();
        }

        private int m_openingCount = 0;
        private AllDataSheetSearchContext m_searchContext;

        private bool m_searching = false;
        public bool Searching
        {
            get { return m_searching; }
            set
            {
                m_searching = value;
                RaisePropertyChanged("Searching");
                m_searchCommand.RaiseCanExecuteChanged();
                m_loadMoreResultCommand.RaiseCanExecuteChanged();
                m_showFavouritesCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged("LoadMoreVisible");
            }
        }

        private string m_searchField;
        public string SearchField
        {
            get { return m_searchField; }
            set
            {
                m_searchField = value;
                RaisePropertyChanged("SearchField");
                m_searchCommand.RaiseCanExecuteChanged();

                if (IsFavouritesMode) m_filteredResults.Refresh();
            }
        }

        private ObservableCollection<PartViewModel> m_searchResults = new ObservableCollection<PartViewModel>();
        public ObservableCollection<PartViewModel> SearchResults
        {
            get { return m_searchResults; }
        }

        private SynchronizedObservableCollection<PartViewModel, Part> m_savedParts;

        private SortDescription m_sortDescription = new SortDescription("LastUseDate", ListSortDirection.Descending);
        private ICollectionView m_filteredResults;
        public ICollectionView FilteredResults
        {
            get { return m_filteredResults; }
        }

        private PartViewModel m_selectedResult;
        public PartViewModel SelectedResult
        {
            get { return m_selectedResult; }
            set { m_selectedResult = value; RaisePropertyChanged("SelectedResult"); }
        }

        private bool m_isFavouritesMode = false;
        public bool IsFavouritesMode
        {
            get { return m_isFavouritesMode; }
            set
            {
                m_isFavouritesMode = value;
                RaisePropertyChanged("IsFavouritesMode");
                RaisePropertyChanged("LoadMoreVisible");
                m_loadMoreResultCommand.RaiseCanExecuteChanged();
            }
        }

        private RelayCommand m_searchCommand;
        public ICommand SearchCommand
        {
            get { return m_searchCommand; }
        }

        private RelayCommand m_openPdfCommand;
        public ICommand OpenPdfCommand
        {
            get { return m_openPdfCommand; }
        }

        private RelayCommand m_loadMoreResultCommand;
        public ICommand LoadMoreResultsCommand
        {
            get { return m_loadMoreResultCommand; }
        }

        private RelayCommand m_addToFavouritesCommand;
        public ICommand AddToFavouritesCommand
        {
            get { return m_addToFavouritesCommand; }
        }

        private RelayCommand m_saveFavouritesCommand;
        public ICommand SaveFavouritesCommand
        {
            get { return m_saveFavouritesCommand; }
        }

        private RelayCommand m_showFavouritesCommand;
        public ICommand ShowFavouritesCommand
        {
            get { return m_showFavouritesCommand; }
        }

        private RelayCommand m_settingsCommand;
        public ICommand SettingsCommand
        {
            get { return m_settingsCommand; }
        }

        private RelayCommand m_requestMoreInfoCommand;
        public ICommand RequestMoreInfoCommand
        {
            get { return m_requestMoreInfoCommand; }
        }

        private RelayCommand m_addCustomCommand;
        public ICommand AddCustomCommand
        {
            get { return m_addCustomCommand; }
        }

        public bool LoadMoreVisible
        {
            get { return !IsFavouritesMode && m_searchContext != null && m_searchContext.CanLoadMore; }
        }

        private void AddResults(List<AllDataSheetPart> results)
        {
            foreach (var item in results)
            {
                PartViewModel viewModel = PartViewModel.FromAllDataSheetPart(item);
                PartViewModel found = m_savedParts.FirstOrDefault(x => x.Code == viewModel.Code);
                if (found != null) viewModel = found;
                viewModel.LoadImage();
                m_searchResults.Add(viewModel);
            }
        }
        
        private async void Search(object param)
        {
            m_searchResults.Clear();
            m_searchContext = null;
            IsFavouritesMode = false;
            Searching = true;
            m_filteredResults.SortDescriptions.Clear();
            m_filteredResults.Refresh();
            Mouse.OverrideCursor = Cursors.AppStarting;

            try
            {
                AllDataSheetSearchResult result = await AllDataSheetPart.SearchAsync(m_searchField);
                m_searchContext = result.SearchContext;
                AddResults(result.Parts);
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringSearchError"), MessageBoxExPredefinedButtons.Ok);
            }

            if(m_openingCount <= 0) Mouse.OverrideCursor = null;
            Searching = false;
        }
        private bool CanSearch(object param)
        {
            return !string.IsNullOrWhiteSpace(m_searchField) && !m_searching;
        }

        private async void OpenPdf(object param)
        {
            if (m_selectedResult == null) return;
            try
            {
                m_openingCount++;
                Mouse.OverrideCursor = Cursors.AppStarting;
                await m_selectedResult.OpenPdf();
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringDownloadError"), MessageBoxExPredefinedButtons.Ok);
            }
            finally
            {
                m_openingCount--;
                if (m_openingCount <= 0) Mouse.OverrideCursor = null;
            }
        }

        private async void LoadMoreResults(object param)
        {
            Searching = true;
            Mouse.OverrideCursor = Cursors.AppStarting;

            try
            {
                AllDataSheetSearchResult result = await AllDataSheetPart.SearchAsync(m_searchContext);
                m_searchContext = result.SearchContext;
                AddResults(result.Parts);
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringSearchError"), MessageBoxExPredefinedButtons.Ok);
            }

            Mouse.OverrideCursor = null;
            Searching = false;
        }
        private bool CanLoadMoreResults(object param)
        {
            return !Searching && !IsFavouritesMode && m_searchContext != null && m_searchContext.CanLoadMore;
        }

        private async void AddToFavourites(object param)
        {
            if (m_selectedResult == null) return;

            if (m_selectedResult.State == PartDatasheetState.Saved)
            {
                if (Global.MessageBox(this, Global.GetStringResource("StringDoYouWantToRemoveFromFavourites"), MessageBoxExPredefinedButtons.YesNo) != MessageBoxExButton.Yes) return;
                m_selectedResult.RemovePdf();
                m_savedParts.Remove(m_selectedResult);
                if (IsFavouritesMode)
                {
                    m_searchResults.Remove(m_selectedResult);
                }
                return;
            }

            try
            {
                m_openingCount++;
                Mouse.OverrideCursor = Cursors.AppStarting;
                PartViewModel part = m_selectedResult;
                await part.SavePdf();
                m_savedParts.Add(part);
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringDownloadError"), MessageBoxExPredefinedButtons.Ok);
            }
            finally
            {
                m_openingCount--;
                if (m_openingCount <= 0) Mouse.OverrideCursor = null;
            }
        }

        private void SaveFavourites(object param)
        {
            Global.SaveSavedParts();
        }

        private void ShowFavourites(object param)
        {
            SearchField = string.Empty;
            IsFavouritesMode = true;
            m_filteredResults.SortDescriptions.Add(m_sortDescription);
            m_filteredResults.Refresh();
            
            m_searchResults.Clear();
            foreach (var item in m_savedParts)
            {
                item.LoadImage();
                m_searchResults.Add(item);
            }
        }
        private bool CanShowFavourites(object param)
        {
            return !Searching;
        }

        private void Settings(object param)
        {
            SettingsViewModel dialogViewModel = new SettingsViewModel();
            Global.Dialogs.ShowDialog(this, dialogViewModel);
        }

        private async void RequestMoreInfo(object param)
        {
            if (m_selectedResult == null) return;
            if (m_selectedResult.MoreInfoState == PartMoreInfoState.Downloading) return;
            else if (m_selectedResult.MoreInfoState == PartMoreInfoState.Available)
            {
                m_selectedResult.PushCopy();
                EditPartViewModel dialogViewModel = new EditPartViewModel(m_selectedResult);
                Global.Dialogs.ShowDialog(this, dialogViewModel);
                if (dialogViewModel.Result == EditPartViewModel.EditPartResult.Ok) m_selectedResult.PopCopy(WorkingCopyResult.Apply);
                else if (dialogViewModel.Result == EditPartViewModel.EditPartResult.Cancel) m_selectedResult.PopCopy(WorkingCopyResult.Restore);
                return;
            }

            try
            {
                m_openingCount++;
                Mouse.OverrideCursor = Cursors.AppStarting;
                await m_selectedResult.RequestMoreInfo();
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringMoreInfoError"), MessageBoxExPredefinedButtons.Ok);
            }
            finally
            {
                m_openingCount--;
                if (m_openingCount <= 0) Mouse.OverrideCursor = null;
            }
        }

        private void AddCustom(object param)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = Global.GetStringResource("StringPdfFiles") + "|*.pdf";
            openFileDialog.ShowDialog(Global.Dialogs.GetWindow(this));
            if (!string.IsNullOrWhiteSpace(openFileDialog.FileName) && File.Exists(openFileDialog.FileName))
            {
                EditPartViewModel dialogViewModel = new EditPartViewModel(openFileDialog.FileName);
                Global.Dialogs.ShowDialog(this, dialogViewModel);
                if (dialogViewModel.Result == EditPartViewModel.EditPartResult.Ok)
                {
                    PartViewModel part = dialogViewModel.Part;
                    m_searchResults.Add(part);
                    m_savedParts.Add(part);
                    part.LastUseDate = DateTime.MinValue;

                    ActionDialogViewModel actionDialogViewModel = new ActionDialogViewModel(part.ComputePagesCount(), Global.GetStringResource("StringCountingPages"));
                    Global.Dialogs.ShowDialog(this, actionDialogViewModel);
                }

                m_filteredResults.Refresh();
            }
        }

        private void RemoveUnavailableSavedParts()
        {
            foreach (string file in Directory.EnumerateFiles(Global.AppDataPath + Path.DirectorySeparatorChar + Global.SavedDatasheetsDirectory))
            {
                if (Path.GetExtension(file) != ".pdf") continue;
                string code = Path.GetFileNameWithoutExtension(file);
                if (m_savedParts.FirstOrDefault(x => x.Code == code || x.CustomPath == file) == null) File.Delete(file);
            }
        }
    }
}
