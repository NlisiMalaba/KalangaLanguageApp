import { render } from '@testing-library/react-native';

import { PlaceholderScreen } from '@/components/placeholder-screen';

describe('PlaceholderScreen', () => {
  it('renders the title and description', () => {
    const { getByText } = render(
      <PlaceholderScreen title="Lessons" description="Catalog coming soon." />,
    );

    expect(getByText('Lessons')).toBeTruthy();
    expect(getByText('Catalog coming soon.')).toBeTruthy();
  });
});
