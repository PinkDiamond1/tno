import React from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';

import {
  CBRAReport,
  ContentLogs,
  DataSource,
  DataSourceDetails,
  DataSourceList,
  MediaType,
  MediaTypeList,
  ReachEarnedMedia,
  Schedule,
  UserList,
} from '.';
import * as styled from './styled';

export const AdminPage: React.FC = () => {
  return (
    <styled.AdminPage>
      <Routes>
        <Route index element={<Navigate to="reports/cbra" />} />
        <Route path="users" element={<UserList />} />
        <Route path="media/types/:id" element={<MediaType />} />
        <Route path="media/types" element={<MediaTypeList />} />
        <Route path="data/sources" element={<DataSourceList />} />
        <Route path="data/sources/:id" element={<DataSource />}>
          <Route index element={<DataSourceDetails />} />
          <Route path="details" element={<DataSourceDetails />} />
          <Route path="schedule" element={<Schedule />} />
          <Route path="metrics" element={<ReachEarnedMedia />} />
        </Route>
        <Route path="contents/log" element={<ContentLogs />} />
        <Route path="reports/cbra" element={<CBRAReport />} />
      </Routes>
    </styled.AdminPage>
  );
};
