﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MVVM.ViewModels;
using Windows.Storage;
namespace TableGameSidekick_Metro.Storages
{
    public class Storage<T> : ViewModelBase<Storage<T>>, TableGameSidekick_Metro.Storages.IStorage<T>
    {
        public Storage(string fileName, StorageFolder folder = null)
        {
            Folder = folder ?? Windows.Storage.ApplicationData.Current.LocalFolder;
            m_FileName = fileName;
            m_Ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));

        }

        protected string m_FileName;
        private System.Runtime.Serialization.Json.DataContractJsonSerializer m_Ser;




        public StorageFolder Folder
        {
            get { return m_Folder.Locate(this).Value; }
            set { m_Folder.Locate(this).SetValueAndTryNotify(value); }
        }
        #region Property StorageFolder Folder Setup
        protected Property<StorageFolder> m_Folder = new Property<StorageFolder>(m_FolderLocator);
        static Func<ViewModelBase, ValueContainer<StorageFolder>> m_FolderLocator =
            RegisterContainerLocator<StorageFolder>(
                "Folder",
                model =>
                    model.m_Folder.Container =
                        model.m_Folder.Container
                        ??
                        new ValueContainer<StorageFolder>("Folder", model));
        #endregion















        protected System.Threading.AutoResetEvent m_BusyWait = new System.Threading.AutoResetEvent(true);

        protected IDisposable CreateBusyLock()
        {
            m_BusyWait.WaitOne();

            var dis = System.Reactive.Disposables.Disposable.Create(() => m_BusyWait.Set());

            return dis;
        }


        public virtual async Task Save()
        {
            using (CreateBusyLock())
            {
                var folder = Folder;
                var file = await GetFileIfExists(folder);


                if (file == null)
                {
                    file = await folder.CreateFileAsync(m_FileName);
                }

                var ms = new MemoryStream();
                m_Ser.WriteObject(ms, this.Value);
                ms.Position = 0;
                await Windows.Storage.FileIO.WriteBytesAsync(file, ms.ToArray());




            }

        }

        private async Task<Windows.Storage.StorageFile> GetFileIfExists(Windows.Storage.StorageFolder folder)
        {

            try
            {
                return await Folder.GetFileAsync(m_FileName);
            }
            catch (FileNotFoundException)
            {

                return null;
            }
        }

        public async Task Refresh()
        {
            using (CreateBusyLock())
            {
                var folder = Folder;
                var file = await GetFileIfExists(folder);
                if (file != null)
                {
                    using (var stream = await file.OpenSequentialReadAsync())
                    {
                        var ms = new MemoryStream();
                        await stream.AsStreamForRead().CopyToAsync(ms);
                        ms.Position = 0;

                        try
                        {
                            var lst = m_Ser.ReadObject(ms);

                            this.Value = (T)(lst);
                        }
                        catch (System.Runtime.Serialization.SerializationException)
                        {


                        }



                    }

                }



            }

        }



        public T Value
        {
            get { return m_Value.Locate(this).Value; }
            set { m_Value.Locate(this).SetValueAndTryNotify(value); }
        }
        #region Property T Value Setup
        protected Property<T> m_Value = new Property<T>(m_ValueLocator);
        static Func<ViewModelBase, ValueContainer<T>> m_ValueLocator =
            RegisterContainerLocator<T>(
                "Value",
                model =>
                    model.m_Value.Container =
                        model.m_Value.Container
                        ??
                        new ValueContainer<T>("Value", model));
        #endregion






















    }


    public class CollectionStorage<T> : Storage<CollectionStorageTray<T>>, IStorage<IEnumerable<T>>
    {
        public CollectionStorage(string fileName, StorageFolder folder = null)
            : base(fileName, folder)
        {
        }


        public new IEnumerable<T> Value
        {
            get
            {
                if (base.Value ==null)
                {
                    base.Value = new CollectionStorageTray<T>();
                }
                return base.Value.Items;
            }
            set
            {
                if (base.Value==null)
                {
                    base.Value = new CollectionStorageTray<T>();
                }
                if (value is IList<T>)
                {
                    base.Value.Items = value as IList<T>;
                }
                else
                {
                    base.Value.Items = value.ToList();
                }
            }
        }
    }

    [DataContract]
    public class CollectionStorageTray<T>
    {

        [DataMember]
        public IList<T> m_Items;

        public IList<T> Items
        {
            get { return m_Items= m_Items?? new List<T>(); }
            set { m_Items = value; }
        }
    }

}
