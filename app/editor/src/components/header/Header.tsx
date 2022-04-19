import { Button } from 'tno-core';
import { useKeycloakWrapper } from 'tno-core';

import * as styled from './styled';

interface IHeaderProps extends React.HTMLAttributes<HTMLDivElement> {
  /**
   * The site name.
   */
  name: string;
}

/**
 * Provides a header element.
 * @param param0 Header element attributes.
 * @returns Header component.
 */
export const Header: React.FC<IHeaderProps> = ({ name, children, ...rest }) => {
  const keycloak = useKeycloakWrapper();

  return (
    <styled.Header {...rest}>
      <div>
        <a href={`${process.env.PUBLIC_URL}/`}>
          <img alt="BC Gov logo" src={process.env.PUBLIC_URL + '/assets/gov_bc_logo.svg'} />
        </a>
      </div>
      <div>
        <div className="title">{name}</div>
        <div className="user">
          <Button onClick={() => keycloak.instance.logout()} name="signOut">
            Sign Out
          </Button>
        </div>
      </div>
    </styled.Header>
  );
};
