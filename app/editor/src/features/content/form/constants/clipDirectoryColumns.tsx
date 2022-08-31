import { IItemModel } from 'hooks/api-editor';
import moment from 'moment';
import {
  FaCloudDownloadAlt,
  FaPaperclip,
  FaPhotoVideo,
  FaPlay,
  FaRegFolder,
  FaTrash,
} from 'react-icons/fa';
import { Column } from 'react-table';
import { Col, Row } from 'tno-core';

/** columns located within file for state manipulation */
export const clipDirectoryColumns = (
  onDelete: Function,
  onSelect: Function,
  onDownload: Function,
  onAttach: Function,
  values: IItemModel,
): Column<IItemModel>[] => [
  {
    id: 'isDirectory',
    Header: () => <div className="list-icon"></div>,
    accessor: 'isDirectory',
    Cell: ({ row, value }) => (
      <div>
        <div className={row.values.isDirectory ? 'hidden' : 'center'}>
          <FaPhotoVideo className="fa-lg" />
        </div>
        <div className={row.values.isDirectory ? 'center' : 'hidden'}>
          <FaRegFolder className="fa-lg" />
        </div>
      </div>
    ),
    maxWidth: 20,
  },
  {
    id: 'name',
    Header: () => <div className="center">Filename</div>,
    accessor: 'name',
    Cell: ({ value }) => <div className="ft-row">{value}</div>,
  },
  {
    id: 'size',
    Header: () => <div className="center">Size</div>,
    accessor: 'size',
    maxWidth: 35,
    Cell: ({ value }) => (
      <div className="ft-row">{!!value ? `${(value / 1000000).toFixed(2)} MB` : ''}</div>
    ),
  },
  {
    id: 'modified',
    Header: () => <div className="center">Modified</div>,
    accessor: 'modified',
    maxWidth: 40,
    Cell: ({ value }) => <div className="ft-row">{moment(value).format('DD-MM-yy hh:mm:ss')}</div>,
  },
  {
    id: 'actions',
    Header: () => <div>Actions</div>,
    accessor: 'isDirectory',
    maxWidth: 30,
    Cell: ({ row, data }: any) => (
      <Row className={`file-actions ${row.values.isDirectory ? 'hidden' : 'center'}`} wrap="nowrap">
        <Col>
          <FaPlay title="watch/listen/edit" onClick={() => onSelect(row.values)} />
        </Col>
        <Col>
          <FaCloudDownloadAlt
            className="fa-lg"
            title="download"
            onClick={() => {
              onDownload(row.values);
            }}
          />
        </Col>
        <Col>
          <FaPaperclip
            className="fa-lg"
            title="Attach to snippet"
            onClick={() => {
              onAttach(row.values);
            }}
          />
        </Col>
        <Col>
          <FaTrash
            className="delete fa-lg"
            title="Delete"
            onClick={() => {
              onDelete(row.values);
            }}
          />
        </Col>
      </Row>
    ),
  },
];
